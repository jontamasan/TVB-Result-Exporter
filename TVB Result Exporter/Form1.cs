using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TVB_Result_Exporter
{
    public partial class Form1 : Form
    {
        // URI to Ferunbus Telemetry
        private const string BaseUri = "http://localhost:37337/";
        private const string WorldUri = BaseUri + "World";
        private const string MissionUri = BaseUri + "Mission";

        // Saved route file path
        private string routeFilePath;
        private string shuttleRouteFilePath;

        private GetTelemetry telemetry;
        private ResultData resultData;

        int tmpBoardingPeopleCount = 0;
        
        private const string appName = "TVB Result Exporter";

        public Form1()
        {
            InitializeComponent();

            this.Text = appName;
            this.ShowInTaskbar = false;
            this.timer1.Interval = 1000;
            this.timer1.Enabled = true;

            telemetry = new GetTelemetry();
            resultData = new ResultData();

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
            File.Delete($"{Directory.GetCurrentDirectory()}/{appName}.tmp");
            Application.Exit();
        }

        // Close button was pressed.
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            File.Delete($"{Directory.GetCurrentDirectory()}/{appName}.tmp");
        }

        // Remove icon from taskbar when minimized.
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
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

        // Copy to clipboard via context menu.
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                Clipboard.SetText(textBox1.Text);
            }
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            string errors = null;
            string jexError = null;

            try
            {
                // World
                var worldResult = await telemetry.GetTelemetryAsync(WorldUri);
                if (worldResult != null)
                {
                    JObject jObject = JObject.Parse(worldResult);
                    JToken dateTimeData = jObject.SelectToken("DateTime");
                    if (dateTimeData != null)
                    {
                        DateTime currentTime = DateTime.Parse(dateTimeData.ToString());
                        // Mission
                        var missionResult = await telemetry.GetTelemetryAsync(MissionUri);
                        if (missionResult != null)
                        {
                            DataExport(missionResult, currentTime);
                        }
                    }
                    //DataExport(worldResult);
                }
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
                jexError = jex.Message;
            }
            finally
            {
                if (errors != null)
                {
                    this.notifyIcon1.Text = $"Waiting for connection to telemetry.";
                }
                else if (jexError != null)
                {
                    this.notifyIcon1.Text = "Json parsing failed";
                }
                else
                {
                    this.notifyIcon1.Text = appName;
                }
            }
        }

        // Calculate the data.
        private void DataExport(string json, DateTime currentTime)
        {
            int passengersTransported = 0;

            using (FileStream fs = File.Create($"{Directory.GetCurrentDirectory()}/{appName}.tmp"))
            using (StreamReader sr = new StreamReader(fs))
            {
                var o = JsonConvert.DeserializeObject<ResultData>(sr.ReadToEnd());
                if (o != null)
                {
                    resultData.ticketsSold = o.ticketsSold;
                    resultData.stopsCompleted = o.stopsCompleted;
                    resultData.scheduledArrival = o.scheduledArrival;
                    resultData.scheduledDeparture = o.scheduledDeparture;
                }
            }

            JObject jObject = JObject.Parse(json);

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

                // Get the reached stops.
                JToken currentStopData = jObject.SelectToken("CurrentStopIndex");
                int currentStopIndex = int.Parse(currentStopData.ToString());
                if (currentStopIndex >= 0)
                {
                    if (!resultData.stopsCompleted.Contains(currentStopIndex))
                    {
                        resultData.stopsCompleted.Add(currentStopIndex);
                    }

                    int boardingPepleCount = int.Parse(jObject.SelectToken($"Stops[{currentStopIndex}].BoardingPeopleCount").ToString());

                    // Tickets were sold if the number of passengers increased after leaving the previous loop.
                    if (tmpBoardingPeopleCount != boardingPepleCount)
                    {
                        // Get ticekts sold.
                        if (tmpBoardingPeopleCount != 0)
                        {
                            if (resultData.ticketsSold.ContainsKey(currentStopIndex))
                            {
                                resultData.ticketsSold[currentStopIndex]++;
                            }
                        }
                    }
                    else
                    {
                        if (!resultData.ticketsSold.ContainsKey(currentStopIndex))
                        {
                            resultData.ticketsSold.Add(currentStopIndex, 0);
                        }
                    }
                    tmpBoardingPeopleCount = boardingPepleCount;

                    for (int i = 0; i < stopsCount; i++)
                    {
                        // Get the Passengers Transported.
                        passengersTransported += int.Parse(jObject.SelectToken($"Stops[{i}].BoardingPeopleCount").ToString());
                    }
                    resultData.passengersTransported = passengersTransported;

                    if (currentTime != null)
                    {
                        // Get the Scheduled arrival;
                        DateTime arrivalTime = DateTime.Parse(jObject.SelectToken($"Stops[{currentStopIndex}].ArrivalTime").ToString());
                        var span = arrivalTime - currentTime;
                        if (span.TotalSeconds > 120)
                        {
                            if (!resultData.scheduledArrival.Contains(currentStopIndex))
                            {
                                resultData.scheduledArrival.Add(currentStopIndex);
                            }
                        }

                        // Get the Scheduled departure;
                        DateTime departureTime = DateTime.Parse(jObject.SelectToken($"Stops[{currentStopIndex}].DepartureTime").ToString());
                        span = departureTime - currentTime;
                        if (span.TotalSeconds > 120)
                        {
                            if (!resultData.scheduledDeparture.Contains(currentStopIndex))
                            {
                                resultData.scheduledDeparture.Add(currentStopIndex);
                            }
                        }
                    }
                    using (StreamWriter sw = new StreamWriter($"{Directory.GetCurrentDirectory()}/{appName}.tmp"))
                    {
                        // Writing to temp file.
                        var j = JsonConvert.SerializeObject(resultData);
                        sw.WriteLine(j);
                    }
                }
            }
            else
            {
                // Maybe start new game. So, initialize variables.
                passengersTransported = 0;
                resultData = new ResultData();
            }

            // Output data.
            var destReached = jObject.SelectToken("DestinationStopReached");
            if (destReached != null)
            {
                if (destReached.ToString() == "true")
                {
                    using (StreamReader sr = new StreamReader($"{Directory.GetCurrentDirectory()}/{appName}.tmp"))
                    using (StreamWriter sw = new StreamWriter($"{Directory.GetCurrentDirectory()}/Result-{appName}.txt"))
                    {

                        var o = JsonConvert.DeserializeObject<ResultData>(sr.ReadToEnd());

                        int ticketsSold = 0;
                        foreach(var ticket in o.ticketsSold)
                        {
                            ticketsSold += ticket.Value;
                        }

                        // Scheduled departure at the last stop is not required.
                        int scheduledDeparture = o.scheduledDeparture.Count > 0 ? o.scheduledDeparture.Count - 1 : 0;

                        this.textBox1.Text = 
                            $"{o.passengersTransported}, {ticketsSold}, _, {o.stopsCompleted.Count}, _, {o.scheduledArrival.Count}, " +
                            $"{scheduledDeparture}, _, _";
                        this.toolStripMenuItem3.Text = this.textBox1.Text;

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

        private void GetKilometres(string folderPath)
        {
        }

    }

    class ResultData
    {
        public int passengersTransported { get; set; }
        public Dictionary<int, int> ticketsSold;
        public List<int> stopsCompleted;
        public List<int> scheduledArrival;
        public List<int> scheduledDeparture;

        public ResultData()
        {
            ticketsSold = new Dictionary<int, int>();
            stopsCompleted = new List<int>();
            scheduledArrival = new List<int>();
            scheduledDeparture = new List<int>();
        }
    }
}
