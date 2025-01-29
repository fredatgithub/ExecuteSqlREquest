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

    public MainWindow()
    {
      InitializeComponent();
      LoadCompletedEventHandler();
    }

    private void LoadCompletedEventHandler()
    {
      // load the connection string from the file connectionId.txt
      if (!File.Exists(ConnectionIdFile))
      {
        MessageBox.Show("Fichier de connexion introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      ConnectionString = File.ReadAllText("connectionId.txt");
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
