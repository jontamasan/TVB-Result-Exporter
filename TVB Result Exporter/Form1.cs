using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TVB_Result_Exporter
{
    public partial class Form1 : Form
    {
        // URI to Ferunbus Telemetry
        private const string BaseUri = "http://localhost:37337/";
        private const string PlayerUri = BaseUri + "Player";
        private const string Vehicles = BaseUri + "Vehicles";
        private const string VehiclesCurrentUri = BaseUri + "Vehicles/Current";
        private const string MissionUri = BaseUri + "Mission";
        private const string MapUri = BaseUri + "Map";
        private const string RouteUri = BaseUri + "Route";
        private const string WorldUri = BaseUri + "World";
        private const string RoadMapUri = BaseUri + "RoadMap";

        // Saved route file path
        private string routeFilePath;
        private string shuttleRouteFilePath;

        private GetTelemetry telemetry;

        private const string appName = "TVB Result Exporter";

        public Form1()
        {
            InitializeComponent();

            this.Text = appName;
            this.ShowInTaskbar = false;
            this.timer1.Interval = 1000;
            this.timer1.Start();

            telemetry = new GetTelemetry();

            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            routeFilePath = basePath + @"\Ferunbus\Saved\Routes\Germany";
            shuttleRouteFilePath = basePath + @"\Fernbus\Saved\ShuttleRoutes\Germany";
        }

        // 'Open' menu.
        private void openFormToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        // Double-click the icon to open the main form.
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        // 'Quit' menu.
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Remove icon from taskbar when minimized.
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            //using (StreamReader sr = new StreamReader(@"E:\_work_\Fernbus jason\Mission (1).json"))
            //{
            //    string json = sr.ReadToEnd();
            //    DateTime dateTime = DateTime.Parse("2020-05-27T19:07:42");
            //    DataExport(json, dateTime);
            //}

            string errors = null;

            try
            {
                // Examine the open tab and connect only that tab.
                // Player

                // Call GetTelemetryAsync method.
                //var playerResult = await telemetry.GetTelemetryAsync(PlayerUri);
                //if (playerResult != null)
                //{
                //    DataExport(playerResult);
                //}

                // Vehicle
                //else if (this.tabControl1.SelectedIndex == (int)Tab.CurrentVehicle)
                //{
                //    this.grid = this.dataGridView2;
                //    var currentVehicleResult = await telemetry.GetTelemetryAsync(VehiclesCurrentUri);
                //    if (currentVehicleResult != null)
                //    {
                //        displayData.Display(await currentVehicleResult.Content.ReadAsStringAsync(),
                //              currentVehicleResult.Headers, this.dataGridView2, this.label1);
                //    }
                //}

                // Mission
                var missionResult = await telemetry.GetTelemetryAsync(MissionUri);
                if (missionResult != null)
                {
                    DataExport(missionResult);
                }


                // Map
                //else if (this.tabControl1.SelectedIndex == (int)Tab.Map)
                //{
                //    this.grid = dataGridView4;
                //    var mapResult = await telemetry.GetTelemetryAsync(MapUri);
                //    if (mapResult != null)
                //    {
                //        displayData.Display(await mapResult.Content.ReadAsStringAsync(),
                //            mapResult.Headers, this.dataGridView4, this.label1);
                //    }
                //}
                //// Route (Navi)
                //else if (this.tabControl1.SelectedIndex == (int)Tab.Route)
                //{
                //    this.grid = dataGridView5;
                //    var routeResult = await telemetry.GetTelemetryAsync(RouteUri);
                //    if (routeResult != null)
                //    {
                //        displayData.Display(await routeResult.Content.ReadAsStringAsync(),
                //            routeResult.Headers, this.dataGridView5, this.label1);
                //    }
                //}
                // World


                var worldResult = await telemetry.GetTelemetryAsync(WorldUri);
                if (worldResult != null)
                {
                    DataExport(worldResult);
                }

                // Road Map
                //else if (this.tabControl1.SelectedIndex == (int)Tab.RoadMap)
                //{
                //    /* roadmapResult is not implementation, since this JSON data is quite
                //     * huge that it is not updated in real time in the game.
                //     * Code needs to be changed significantly when implementing. */
                //}
            }
            catch (HttpRequestException httpex)
            {
                // 404 error, name resolution failure, etc.
                // Show exception messages recursively, including InnerException.
                Exception ex = httpex;
                while (ex != null)
                {
                    errors += $"{ex.Message}\n";
                    ex = ex.InnerException;
                }
            }
            catch (TaskCanceledException tcex)
            {
                // When tasks are canceled (e.g. timeout).
                errors = tcex.Message;
            }
            catch (JsonReaderException jex)
            {
                // Defect in json data etc.
                errors = jex.Message;
            }
            finally
            {
                if (errors != null)
                {
                    this.notifyIcon1.Text = "Waiting for connection to telemetry.";
                }
                else
                {
                    this.notifyIcon1.Text = appName;
                }
            }
        }

        // Calculate the data.
        private void DataExport(string json)
        {
            int passengersTransported = 0;
            int ticketsSould; // not implemented yet
            int invalidCheckedIn; // not implemented yet
            int stopsCompleted = 0;
            int kilometersDriven; // not implemented yet
            int scheduledArrive = 0;
            int scheduledDeperture = 0;
            int accidents; // not implemented yet
            int raderControl; // not implemented yet

            JObject jObject = JObject.Parse(json);
            DateTime currentTime = new DateTime(0);

            // Get the current time of the game from World.json.
            JToken dateTimeData = jObject.SelectToken("DateTime");
            if (dateTimeData != null)
            {
                currentTime = DateTime.Parse(dateTimeData.ToString());
            }

            JToken stopsData = jObject.SelectToken("Stops");
            if (stopsData != null)
            {
                // Get the number of stops.
                int stopsCount = stopsData.Count();
                bool[] stops = new bool[stopsCount];
                // Get the reached stops.
                JToken currentStopData = jObject.SelectToken("CurrentStopIndex");
                int currentStopIndex = int.Parse(currentStopData.ToString());
                if (currentStopIndex >= 0)
                {
                    stops[currentStopIndex] = true;
                }

                for (int i = 0; i < stopsCount; i++)
                {
                    // Get the Passengers Transported.
                    passengersTransported += int.Parse(jObject.SelectToken($"Stops[{i}].BoardingPeopleCount").ToString());

                    // Get the Stops Completed.
                    if (stops[i])
                    {
                        stopsCompleted++;
                    }

                    if (currentTime != null) // DataTableにして記録済みにフラグを立てたほうがいいのでは。セーブして再開した場合について。
                    {
                        // Get the Scheduled arrival;
                        DateTime arrivalTime = DateTime.Parse(jObject.SelectToken($"Stops[{i}].ArrivalTime").ToString());
                        var span = arrivalTime - currentTime;
                        if (span.TotalSeconds <= 0)
                        {
                            scheduledArrive++;
                        }
                        // Get the Scheduled departure;
                        DateTime departureTime = DateTime.Parse(jObject.SelectToken($"Stops[{i}].DepartureTime").ToString());
                        span = departureTime - currentTime;
                        if (span.TotalSeconds <= 0)
                        {
                            scheduledDeperture++;
                        }
                    }
                }
            }

            // Output data.
            var destReached = jObject.SelectToken("DestinationStopReached");
            if (destReached != null)
            {
                if (destReached.ToString() == "true")
                {
                    this.textBox1.Text = $"{passengersTransported}, _, _, {stopsCompleted}, _, {scheduledArrive}, {scheduledDeperture}, _, _";
                    this.toolStripMenuItem3.Text = this.textBox1.Text;

                    using (StreamWriter sw = new StreamWriter($"{Directory.GetCurrentDirectory()}/Result.{appName}.txt"))
                    {
                        sw.WriteLine(textBox1.Text);
                        sw.WriteLine();
                        sw.WriteLine("All you need to submit is:\n" +
                            "- Passengers transported\n" +
                            "- Tickets sold\n" +
                            "- Checked in/sold invalid tickets\n" +
                            "- Stops completed\n" +
                            "- Kilometres driven\n" +
                            "- Scheduled departure\n" +
                            "- Scheduled arrival\n" +
                            "- Accidents\n" +
                            "- Radar control");
                    }
                }
                else
                {
                    this.textBox1.Text = "Calculating...";
                    this.toolStripMenuItem3.Text = this.textBox1.Text;
                }
            }

        }

        // Double clicking on the text box will select all text.
        private void textBox1_DoubleClick(object sender, EventArgs e)
        {
            textBox1.SelectAll();
        }

        // Copy to Clipboard.
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                Clipboard.SetText(textBox1.Text);
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                Clipboard.SetText(textBox1.Text);
            }
        }

        private void GetKilometres(string folderPath)
        {

        }
    }
}
