using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using amadeus;
using amadeus.util;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using amadeus.resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Runtime.Caching;

namespace AmadeusLetovi
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (ddlDolazak.Items.Count == 0)
            {
                try
                {
                    //IATA šifre uzete sa stranice US Department of transportation: https://www.transportation.gov/policy/aviation-policy/airport-codes-txt
                    var path = Page.Server.MapPath(@"~\airport_codes.txt");
                    List<ListItem> podatak = new List<ListItem>();
                    //IATA kodovi i imena gradova u format s kojim se može raditi
                    foreach (string line in File.ReadLines(path, Encoding.UTF8))
                    {
                        string ddlVrijednost = Regex.Match(line, "^.{0,3}").ToString();
                        string ddlTekst = Regex.Match(line, "^.{0,3}").ToString() + " (" + Regex.Match(line, "(?<=\\\\)(.*)(?=\\\\)").ToString() + ")";

                        podatak.Add(new ListItem(ddlTekst, ddlVrijednost));
                    }
                    ddlPolazak.Items.AddRange(podatak.ToArray());
                    ddlPolazak.Items.Insert(0, new ListItem("Odaberite aerodrom", String.Empty));
                    ddlDolazak.Items.AddRange(podatak.ToArray());
                    ddlDolazak.Items.Insert(0, new ListItem("Odaberite aerodrom", String.Empty));
                }
                catch (IOException ioE)
                {
                    Debug.WriteLine("Greška u čitanju datoteke/loadanju u DDL-ove: " + ioE.Message);
                }
            }
        }

        protected void btnPretragaLetova_Click(object sender, EventArgs e)
        {
            string Od = ddlPolazak.SelectedItem.Value;
            string Do = ddlDolazak.SelectedItem.Value;
            string Valuta = ddlValuta.SelectedValue;
            string brojPutnika = txtBrojPutnika.Text;
            string Datum = datePickerBoot.Value;
            string DatumPovratka = datePickerBootPovratak.Value;

            if (txtBrojPutnika.Text == string.Empty)
                txtBrojPutnika.Text = "1";
            //Ako nije upisan, uzimamo današnji
            if (Datum == string.Empty)
            {
                string m = DateTime.Today.Month < 10 ? "0" + DateTime.Today.Month : DateTime.Today.Month.ToString();
                string d = DateTime.Today.Day < 10 ? "0" + DateTime.Today.Day : DateTime.Today.Day.ToString();
                Datum = DateTime.Today.Year + "-" + m + "-" + d;
            }

            string podaci = GetLocations(Od, Do, Datum, DatumPovratka, brojPutnika, Valuta);
            if (podaci == string.Empty)
                lblGreska.Text = "Ne postoje podaci za zadate parametre pretraživanja.";
            else
            {
                lblGreska.Text = string.Empty;
                dynamic data = JArray.Parse(podaci);
                int count = data.Count;

                DataTable table = new DataTable();
                table.Columns.Add("Polazak");
                table.Columns.Add("Dolazak");
                table.Columns.Add("Datum polaska");
                table.Columns.Add("Datum dolaska");
                table.Columns.Add("Broj presjedanja");
                table.Columns.Add("Slobodna mjesta");
                table.Columns.Add("Cijena"); ;
                DataRow row;
                for (int i = 0; i < count; i++)
                {
                    //Važan jer je broj presjedanja = broju segmenata
                    int brojPresjedanja = data[i].itineraries[0].segments.Count;
                    string putPresjedanja = data[i].itineraries[0].segments[0].departure.iataCode + "-";
                    for (int x = 0; x < brojPresjedanja; x++)
                    {
                        putPresjedanja += data[i].itineraries[0].segments[x].arrival.iataCode + "-";
                    }
                    putPresjedanja = putPresjedanja.Remove(putPresjedanja.Length - 1);
                    row = table.NewRow();
                    row["Polazak"] = data[i].itineraries[0].segments[0].departure.iataCode;
                    row["Dolazak"] = data[i].itineraries[0].segments[brojPresjedanja - 1].arrival.iataCode;
                    row["Datum polaska"] = data[i].itineraries[0].segments[0].departure.at;
                    row["Datum dolaska"] = data[i].itineraries[0].segments[brojPresjedanja - 1].arrival.at;
                    if (brojPresjedanja == 1)
                        row["Broj presjedanja"] = "Direktan let";
                    else
                        row["Broj presjedanja"] = brojPresjedanja - 1 + " (" + putPresjedanja + ")";
                    row["Slobodna mjesta"] = data[i].numberOfBookableSeats;
                    row["Cijena"] = data[i].price.total + " " + data[i].price.currency;

                    table.Rows.Add(row);
                }
                table.AcceptChanges();
                gvJsonPodaciZaPrikaz.DataSource = table;
                gvJsonPodaciZaPrikaz.DataBind();
            }
        }

        public static string GetLocations(string Od, string Do, string Datum, string DatumPovratka, string BrojPutnika, string Valuta)
        {
            try
            {
                //Nije tajna, free account je
                var apikey = "qUuQuLKV7fGRo1Li31RydErkBGbzuLzK";
                var apisecret = "U9RUrTJCAHpLGOE2";

                string result;
                MemoryCache cache = MemoryCache.Default;
                bool ifExists = cache.Contains(Od + Do + Datum + BrojPutnika + Valuta);
                //Ako postoji u cache-u dohvati ga od tamo
                if (ifExists)
                {
                    result = cache.GetCacheItem(Od + Do + Datum + BrojPutnika + Valuta).Value.ToString();
                }
                //Ako ga nema u cache-u, zovi api
                else
                {
                    Amadeus amadeus = Amadeus.builder(apikey, apisecret).build();

                    //Zbog povratnog leta moramo ovako odvojiti pozive, jer čim se on spominje, iako je prazan, api vraća samo povratne letove...
                    //https://developers.amadeus.com/self-service/category/air/api-doc/flight-offers-search/api-reference
                    //returnDate
                    //If this parameter is specified, only round-trip itineraries are found.
                    if (DatumPovratka == string.Empty)
                    {
                        FlightOffer[] flightOffers = amadeus.shopping.flightOffers.get(Params
                        .with("originLocationCode", Od)
                        .and("destinationLocationCode", Do)
                        .and("departureDate", Datum)
                        .and("adults", BrojPutnika)
                        .and("currencyCode", Valuta));
                        var kesKey = (Od + Do + Datum + BrojPutnika + Valuta).ToString();
                        var kesvalue = flightOffers[0].response.data.ToString();
                        var kesPolicy = new CacheItemPolicy { SlidingExpiration = new TimeSpan(2, 0, 0) };
                        CacheItem zapis = new CacheItem(kesKey, kesvalue);
                        cache.Add(zapis, kesPolicy);
                        result = kesvalue;
                    }
                    else
                    {
                        FlightOffer[] flightOffers = amadeus.shopping.flightOffers.get(Params
                        .with("originLocationCode", Od)
                        .and("destinationLocationCode", Do)
                        .and("departureDate", Datum)
                        .and("adults", BrojPutnika)
                        .and("currencyCode", Valuta)
                        .and("returnDate", DatumPovratka));
                        var kesKey = (Od + Do + Datum + BrojPutnika + Valuta).ToString();
                        var kesvalue = flightOffers[0].response.data.ToString();
                        var kesPolicy = new CacheItemPolicy { SlidingExpiration = new TimeSpan(2, 0, 0) };
                        CacheItem zapis = new CacheItem(kesKey, kesvalue);
                        cache.Add(zapis, kesPolicy);
                        result = kesvalue;
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Greška: " + e.Message.ToString());
                return String.Empty;
            }
        }
    }
}