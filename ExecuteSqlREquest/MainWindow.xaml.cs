using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Npgsql;

namespace ExecuteSqlREquest
{
  /// <summary>
  /// Logique d'interaction pour MainWindow.xaml 
  /// </summary>
  public partial class MainWindow: Window
  {
    private string ConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=your_password;Database=your_database";
    private const string ConnectionIdFile = "connectionId.txt";
    private const string WindowSettingsFile = "windowSettings.txt";

    public MainWindow()
    {
      InitializeComponent();
      LoadConnexionId();
    }

    private void LoadConnexionId()
    {
      // load the connection string from the file connectionId.txt
      if (!File.Exists(ConnectionIdFile))
      {
        MessageBox.Show("Fichier de connexion introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      ConnectionString = File.ReadAllText("connectionId.txt");
    }

    private void Window_SourceInitialized(object sender, EventArgs e)
    {
      LoadWindowSettings();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      SaveWindowSettings();
    }

    private void LoadWindowSettings()
    {
      try
      {
        if (File.Exists(WindowSettingsFile))
        {
          string[] settings = File.ReadAllLines(WindowSettingsFile);
          if (settings.Length >= 4)
          {
            this.Left = double.Parse(settings[0]);
            this.Top = double.Parse(settings[1]);
            this.Width = double.Parse(settings[2]);
            this.Height = double.Parse(settings[3]);
          }
        }
      }
      catch (Exception ex)
      {
        // En cas d'erreur, on utilise les dimensions par défaut
        MessageBox.Show($"Erreur lors du chargement des paramètres de la fenêtre : {ex.Message}", 
                       "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void SaveWindowSettings()
    {
      try
      {
        string[] settings = new string[]
        {
          this.Left.ToString(),
          this.Top.ToString(),
          this.Width.ToString(),
          this.Height.ToString()
        };
        File.WriteAllLines(WindowSettingsFile, settings);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Erreur lors de la sauvegarde des paramètres de la fenêtre : {ex.Message}", 
                       "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void ExecuteQuery_Click(object sender, RoutedEventArgs e)
    {
      string query = QueryTextBox.Text.Trim();

      if (string.IsNullOrWhiteSpace(query))
      {
        MessageBox.Show("Veuillez entrer une requête SQL.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      if (query.StartsWith("SELECT *", StringComparison.OrdinalIgnoreCase))
      {
        query += " LIMIT 200";
      }

      QueryProgressBar.Visibility = Visibility.Visible;

      try
      {
        await ExecuteQueryAsync(query);
      }
      catch (NpgsqlException ex) when (ex.Message.Contains("password"))
      {
        MessageBox.Show("Erreur de connexion : mot de passe invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      catch (NpgsqlException ex)
      {
        MessageBox.Show($"Erreur SQL : {ex.Message}", "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Erreur inconnue : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      finally
      {
        QueryProgressBar.Visibility = Visibility.Collapsed;
      }
    }

    private Task ExecuteQueryAsync(string query)
    {
      return Task.Run(() =>
      {
        using (var conn = new NpgsqlConnection(ConnectionString))
        {
          conn.Open();

          using (var cmd = new NpgsqlCommand(query, conn))
          {
            if (query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
              var dataAdapter = new NpgsqlDataAdapter(cmd);
              var dataTable = new DataTable();
              dataAdapter.Fill(dataTable);

              Dispatcher.Invoke(() =>
              {
                ResultDataGrid.ItemsSource = dataTable.DefaultView;
              });
            }
            else
            {
              int affectedRows = cmd.ExecuteNonQuery();
              Dispatcher.Invoke(() =>
              {
                MessageBox.Show($"{affectedRows} ligne(s) affectée(s).", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
              });
            }
          }
        }
      });
    }
  }
}
