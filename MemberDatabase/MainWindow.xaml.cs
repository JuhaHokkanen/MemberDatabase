using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows;

namespace MemberDatabase;

public partial class MainWindow : Window
{
    
    private const string ConnectionString = "mongodb://localhost:27017";

    private const string DbName = "MemberDatabaseDb";

    private const string CollectionName = "members";

    private IMongoCollection<Member>? _col;
    private Member? _current;

    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = MongoClientSettings.FromConnectionString(ConnectionString);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            var client = new MongoClient(settings);

            var db = client.GetDatabase(DbName);
            _col = db.GetCollection<Member>(CollectionName);

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Yhteys epäonnistui: " + ex.Message);
        }
    }

    private async Task ReloadAsync()
    {
        if (_col == null) return;

        try
        {
            var list = await _col.Find(FilterDefinition<Member>.Empty).ToListAsync();
            Grid.ItemsSource = null; // Tämä auttaa Gridin päivityksessä
            Grid.ItemsSource = list;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Virhe ladattaessa tietoja: {ex.Message}");
        }
    }
    private async void OnAdd(object sender, RoutedEventArgs e)
    {
        if (_col == null) return;
        try
        {
            var m = ReadForm();
            Validate(m);
            await _col.InsertOneAsync(m);
            await ReloadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private async void OnUpdate(object sender, RoutedEventArgs e)
    {
        if (_col == null || _current == null) { MessageBox.Show("Valitse rivi."); return; }
        try
        {
            var m = ReadForm();
            m.Id = _current.Id;
            Validate(m);
            await _col.ReplaceOneAsync(x => x.Id == m.Id, m);
            await ReloadAsync();
            OnClear(null, null); // Tyhjennetään lomake päivityksen jälkeen
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (_col == null || _current == null) return;
        if (MessageBox.Show("Poistetaanko valittu jäsen?", "Vahvistus", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        await _col.DeleteOneAsync(x => x.Id == _current.Id);
        await ReloadAsync();
    }

    private void OnSelect(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _current = Grid.SelectedItem as Member;
        if (_current == null) return;
        TbFirst.Text = _current.FirstName;
        TbLast.Text = _current.LastName;
        TbEmail.Text = _current.Email;
        TbPhone.Text = _current.Phone;
        TbPostal.Text = _current.PostalCode;
        TbAddress.Text = _current.Address;
        DpStart.SelectedDate = _current.MembershipStart;
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        _current = null;
        TbFirst.Text = TbLast.Text = TbEmail.Text = TbPhone.Text = TbPostal.Text = TbAddress.Text = string.Empty;
        DpStart.SelectedDate = DateTime.Today;
        Grid.UnselectAll();
    }

    private Member ReadForm() => new Member
    {
        FirstName = (TbFirst.Text ?? string.Empty).Trim(),
        LastName = (TbLast.Text ?? string.Empty).Trim(),
        Address = (TbAddress.Text ?? string.Empty).Trim(),
        PostalCode = (TbPostal.Text ?? string.Empty).Trim(),
        Phone = (TbPhone.Text ?? string.Empty).Trim(),
        Email = (TbEmail.Text ?? string.Empty).Trim(),
        MembershipStart = DpStart.SelectedDate ?? DateTime.Today
    };

    private static void Validate(Member m)
    {
        if (string.IsNullOrWhiteSpace(m.FirstName)) throw new Exception("Etunimi puuttuu.");
        if (string.IsNullOrWhiteSpace(m.LastName)) throw new Exception("Sukunimi puuttuu.");
        if (!Regex.IsMatch(m.PostalCode ?? string.Empty, @"^\d{5}$")) throw new Exception("Postinumero virheellinen.");
        try { var _ = new System.Net.Mail.MailAddress(m.Email); } catch { throw new Exception("Sähköposti ei kelpaa."); }
    }

    public class Member
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime MembershipStart { get; set; } = DateTime.Today;
    }
}
