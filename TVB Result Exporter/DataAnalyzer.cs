using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVB_Result_Exporter
{
    class DataAnalyzer
    {
        public DataTable Analyze(string jsonData)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add();
            dataTable.Columns.Add();

            // Parsing the Json data and adding data to the DataTable two columns at a time.
            JToken json = JToken.Parse(jsonData);
            IEnumerable<KeyValuePair<string, JValue>> fields = new JsonFieldsCollector(json).GetAllFields();

            if (fields.Count() > 0)
            {
                foreach (var field in fields)
                {
                    dataTable.Rows.Add(field.Key, field.Value);
                }
            }
            else
            {
                dataTable.Rows.Add("No data");
            }

            return dataTable;
        }

        /// <summary>
        /// Collect all fields of JSON object.
        /// https://riptutorial.com/csharp/example/32164/
        /// </summary>
        class JsonFieldsCollector
        {
            private readonly Dictionary<string, JValue> fields;

            public JsonFieldsCollector(JToken token)
            {
                this.fields = new Dictionary<string, JValue>();
                CollectFields(token);
            }

            private void CollectFields(JToken jToken)
            {
                switch (jToken.Type)
                {
                    case JTokenType.Object:
                        foreach (JProperty child in jToken.Children<JProperty>())
                            CollectFields(child);
                        break;
                    case JTokenType.Array:
                        foreach (JToken child in jToken.Children())
                            CollectFields(child);
                        break;
                    case JTokenType.Property:
                        CollectFields(((JProperty)jToken).Value);
                        break;
                    default:
                        this.fields.Add(jToken.Path, (JValue)jToken);
                        break;
                }
            }

            public IEnumerable<KeyValuePair<string, JValue>> GetAllFields() => this.fields;
        }
    }
}
