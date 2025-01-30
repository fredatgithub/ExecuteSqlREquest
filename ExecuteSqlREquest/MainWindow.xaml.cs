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
    private const string QueryContentFile = "lastQuery.txt";
    private PleaseWaitWindow _pleaseWaitWindow;

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
      LoadQueryContent();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      SaveWindowSettings();
      SaveQueryContent();
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
            Left = double.Parse(settings[0]);
            Top = double.Parse(settings[1]);
            Width = double.Parse(settings[2]);
            Height = double.Parse(settings[3]);
          }
        }
      }
      catch (Exception exception)
      {
        // En cas d'erreur, on utilise les dimensions par défaut
        MessageBox.Show($"Erreur lors du chargement des paramètres de la fenêtre : {exception.Message}", 
                       "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void SaveWindowSettings()
    {
      try
      {
        string[] settings = new string[]
        {
          Left.ToString(),
          Top.ToString(),
          Width.ToString(),
          Height.ToString()
        };
        File.WriteAllLines(WindowSettingsFile, settings);
      }
      catch (Exception exception)
      {
        MessageBox.Show($"Erreur lors de la sauvegarde des paramètres de la fenêtre : {exception.Message}", 
                       "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void LoadQueryContent()
    {
      try
      {
        if (File.Exists(QueryContentFile))
        {
          QueryTextBox.Text = File.ReadAllText(QueryContentFile);
        }
      }
      catch (Exception exception)
      {
        MessageBox.Show($"Erreur lors du chargement de la dernière requête : {exception.Message}", 
                       "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void SaveQueryContent()
    {
      try
      {
        File.WriteAllText(QueryContentFile, QueryTextBox.Text);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Erreur lors de la sauvegarde de la requête : {ex.Message}", 
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
        if (query.EndsWith(";"))
        {
          query = query.TrimEnd(';');
        }

        query += " LIMIT 200;";
      }

      _pleaseWaitWindow = new PleaseWaitWindow { Owner = this };
      _pleaseWaitWindow.Show();
      ExecuteButton.IsEnabled = false;

      try
      {
        await ExecuteQueryAsync(query);
      }
      catch (NpgsqlException exception) when (exception.Message.Contains("password"))
      {
        MessageBox.Show("Erreur de connexion : mot de passe invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      catch (NpgsqlException exception)
      {
        MessageBox.Show($"Erreur SQL : {exception.Message}", "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      catch (Exception exception)
      {
        MessageBox.Show($"Erreur inconnue : {exception.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      finally
      {
        if (_pleaseWaitWindow != null)
        {
          _pleaseWaitWindow.Close();
          _pleaseWaitWindow = null;
        }
        ExecuteButton.IsEnabled = true;
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
