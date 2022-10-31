using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace JsonTextViewer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnFileChooser_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Title = "Browse JSON Files";

            openFileDialog1.Filter = "Json files (*.json)|*.json|Text files (*.txt)|*.txt";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = openFileDialog1.FileName;
            }
        }

        private void btnReadFile_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtFilePath.Text.Trim()))
            {
                using (StreamReader r = new StreamReader(txtFilePath.Text.Trim()))
                {
                    string jsonText = r.ReadToEnd();

                    if (!string.IsNullOrEmpty(jsonText))
                    {
                        dgJsonText.DataSource = GenerateDataTable(jsonText);
                    }
                }
            }
            else
            {
                MessageBox.Show("No file found !", "Json Viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtFilePath.Text = string.Empty;

            dgJsonText.DataSource = null;

            txtFilePath.Focus();
        }

        private DataTable GenerateDataTable(string json)
        {
            DataTable dtResult = new DataTable();

            try
            {
                var ParsedText = JObject.Parse(json);

                string ResourceType = ParsedText["resourceType"].ToString();
                string Type = ParsedText["type"].ToString();

                dtResult.Columns.AddRange(new[]
                    {
                    new DataColumn("URN"),
                    new DataColumn("Gender"),
                    new DataColumn("Age"),
                    new DataColumn("Race"),
                    new DataColumn("Ethinicity"),
                    new DataColumn("MotherMaidenName"),
                    new DataColumn("DeceasedDateTime"),
                    new DataColumn("Address"),
                    });

                var EntryNodeVal = ParsedText["entry"];

                EntryNodeVal.All(entryVal =>
                {
                    dtResult.Rows.Add(entryVal["fullUrl"] == null ? "" : (entryVal["fullUrl"].ToString()).Replace("urn:uuid:", ""));

                    return true;
                });

                dtResult.AcceptChanges();

                DataRow dRow = null;

                EntryNodeVal.All(entryVal =>
                {
                    string URN = entryVal["fullUrl"] == null ? "" : (entryVal["fullUrl"].ToString()).Replace("urn:uuid:", "");

                    dRow = dtResult.Select("URN = '" + URN + "'")[0];

                    var resourceValue = JObject.Parse(entryVal["resource"].ToString());

                    dRow["Gender"] = resourceValue["gender"] == null ? "" : resourceValue["gender"].ToString();
                    dRow["DeceasedDateTime"] = resourceValue["deceasedDateTime"] == null ? "" : resourceValue["deceasedDateTime"].ToString();

                    var Extension = JsonConvert.SerializeObject(resourceValue["extension"]);

                    if (Extension != "null")
                    {
                        var ExtnArray = JArray.Parse(Extension);

                        ExtnArray.All(ExtnValue =>
                        {
                            var Extension1 = JsonConvert.SerializeObject(ExtnValue["extension"]);
                            var ValueString = JsonConvert.SerializeObject(ExtnValue["valueString"]);
                            var ValueAddress = JsonConvert.SerializeObject(ExtnValue["valueAddress"]);
                            var ValueDecimal = JsonConvert.SerializeObject(ExtnValue["valueDecimal"]);

                            if (Extension1 != null && Extension1 != "null")
                            {
                                var ExtnArray1 = JArray.Parse(Extension1);

                                if (ExtnArray1 != null)
                                {
                                    dRow["Race"] = ExtnArray1[1]["valueString"] == null ? "" : ExtnArray1[1]["valueString"].ToString();
                                }
                            }
                            else if (ValueString != null && ValueString != "null")
                            {
                                if (JsonConvert.SerializeObject(ExtnValue["url"]).Contains("mother"))
                                {
                                    dRow["MotherMaidenName"] = JsonConvert.DeserializeObject(ValueString);
                                }
                                else
                                {
                                    dRow["Ethinicity"] = JsonConvert.DeserializeObject(ValueString);
                                }
                            }
                            else if (ValueAddress != null && ValueAddress != "null")
                            {
                                var Addr = JObject.Parse(JsonConvert.DeserializeObject(ValueAddress).ToString());

                                if (Addr != null)
                                {
                                    dRow["Address"] = Addr["city"].ToString() + ", " + Addr["state"].ToString() + ", " + Addr["country"].ToString();
                                }
                            }
                            else if (ValueDecimal != null && ValueDecimal != "null")
                            {
                                if (JsonConvert.SerializeObject(ExtnValue["url"]).Contains("quality"))
                                {
                                    dRow["Age"] = JsonConvert.DeserializeObject(Math.Round(Convert.ToDouble(ValueDecimal)).ToString());
                                }
                            }

                            return true;
                        });

                        string Identifier = resourceValue["identifier"].ToString();
                        string Name = resourceValue["name"].ToString();
                        string Telecom = resourceValue["telecom"].ToString();
                        string MaritalStatus = resourceValue["maritalStatus"].ToString();
                        string Communication = resourceValue["communication"].ToString();

                        var request = JsonConvert.DeserializeObject<JToken>(entryVal["request"].ToString());

                        request.All(requestValue =>
                        {
                            return true;
                        });
                    }

                    dtResult.AcceptChanges();

                    return true;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Json Viewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dtResult;
        }
    }
}
