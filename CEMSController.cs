using System;
using System.Data;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using MySql.Data.MySqlClient;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using WebApiPoster.PCR;
using System.Reflection;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using Parse;
using System.Threading.Tasks;
using System.Text;
using System.Drawing;

namespace WebApiPoster.Controllers
    {
    public class CEMSController : ApiController
        {
        private static string ParseUserName = "TestUser";
        private static string ParsePassword = "TestPass";

        public IHttpActionResult Get()
            {
            return Ok();
            }
        // localhost:api/CEMS/GetCads
        [HttpGet]
        public string GetCads(string device_id = "%", string cad_id = "%", string bus_id="%")
            {
            if (device_id == "%" && cad_id == "%" && bus_id=="%")
                return "";
            ExtendedDataTable dt = GetCadTable(device_id, cad_id, bus_id);
            List<object> myList = dt.toList();  //new List<object>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //     myList.Add(dr.Table);
            //}
            //string json = new JavaScriptSerializer().Serialize(myList);
            string json = JsonConvert.SerializeObject(myList);
            //UpdateDownloadedCad(device_id, cad_id);
            //System.IO.File.WriteAllText("c:\\temp\\Output.json", json);
            return json;
            }
       
        [HttpGet]
        public string GetCadsRaw(string device_id = "%", string cad_id = "%", string bus_id = "%")
            {
            if (device_id == "%" && cad_id == "%" && bus_id == "%") return "";
            ExtendedDataTable dt = GetCadTable(device_id, cad_id, bus_id);
            List<object> myList = new List<object>();
            foreach (DataRow dr in dt.Rows)
                {
                myList.Add(dr.ItemArray);
                }

            string json = JsonConvert.SerializeObject(myList);
            return json;
            }
        [HttpGet]
        public string GetNewChange(string change_id = "%")
        {
             New_Change newchange = new New_Change(change_id);
             string Json_String = "{" + JsonMaker.GetJSON(newchange)+"}";

             Logger.LogAction(((JObject)JsonConvert.DeserializeObject(Json_String)).ToString(), "JSON_Gets");
             return Json_String;
        }

        [HttpGet]
        public string GetPCR(string pcr_id = "%")
            {
            Utilities.UseRequiredFields = true;
            StringBuilder Pcr_Json = new StringBuilder();

            Pcr pcr = new Pcr(pcr_id);
            Pcr_Json.Append(JsonMaker.GetJSON(pcr) + System.Environment.NewLine);

            Dispatch Dispatch = new Dispatch(pcr.pcr_dispatch_id);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Dispatch) + System.Environment.NewLine);

            List<Members> MembersList = Utilities.GetClassList<Members>("pcr_members", pcr_id, "pcr_id");
            Pcr_Json.Append("," + JsonMaker.GetJSONFromList(MembersList, Prefix:"pcr_members") + System.Environment.NewLine);
           
            Demographic Demographic = new Demographic(pcr.pcr_demographic_id);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Demographic) + System.Environment.NewLine);

            Assessment Assessment = new Assessment(pcr.pcr_assessment_id);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Assessment) + System.Environment.NewLine);

            Narrative_Notes Narrative_Notes = new Narrative_Notes(pcr.pcr_narrative_notes_id);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Narrative_Notes) + System.Environment.NewLine);

            Rma Rma = new Rma(pcr.pcr_rma_id);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Rma) + System.Environment.NewLine);

            Apcf Apcf = new Apcf(pcr.pcr_apcf_id);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Apcf) + System.Environment.NewLine);

            Disposition Disposition = new Disposition(pcr.pcr_disposition_id);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Disposition) + System.Environment.NewLine);

            Ems_run Ems_Run = new Ems_run(pcr.ems_run);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Ems_Run) + System.Environment.NewLine);

            Narcotic Narcotic = new Narcotic(pcr.pcr_narcotics_id);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Narcotic) + System.Environment.NewLine);

            PCR.Authorization Authorization = new PCR.Authorization(pcr.pcr_authorization_id);
            Pcr_Json.Append("," + JsonMaker.GetJSON(Authorization));

            string SelectQuery = "(SELECT a.* FROM pcr_buttons a inner join all_buttons b on a.button_id = b.id inner join sections c on b.section_id = c.id) buttons";
            List<Buttons> ButtonsList = Utilities.GetClassList<Buttons>(SelectQuery, pcr_id, "pcr_id");
            Pcr_Json.Append("," + JsonMaker.GetJSONFromList(ButtonsList, Prefix: "pcr_buttons") + System.Environment.NewLine);

            SelectQuery = "(SELECT a.* FROM pcr_inputs a inner join all_buttons b on a.input_id = b.id inner join sections c on b.section_id = c.id) inputs";
            List<Inputs> InputsList = Utilities.GetClassList<Inputs>(SelectQuery, pcr_id, "pcr_id");
            Pcr_Json.Append("," + JsonMaker.GetJSONFromList(InputsList, Prefix: "pcr_inputs") + System.Environment.NewLine);

            Pcr_Json.Insert(0, "{");
            Pcr_Json.Append("}");
            Logger.LogAction(((JObject)JsonConvert.DeserializeObject(Pcr_Json.ToString())).ToString (), "JSON_Gets");
            return Pcr_Json.ToString();
            }


        private void UpdateDownloadedCad(string device_id = "%", string cad_id = "%")
            {
            try
                {

                using (MySqlConnection cn = new MySqlConnection(DbConnect.ConnectionString))
                    {
                    cn.Open();
                    string UpdateSql = "update cad_number set downloaded_time = CURRENT_TIMESTAMP() where (ifnull(downloaded_time,0)=0 or year(downloaded_time)=1970) and id like '" + cad_id + "' and agency_id like '" + device_id + "'";
                    MySqlCommand cmd = new MySqlCommand(UpdateSql, cn);
                    cmd.ExecuteNonQuery();
                    }
                }
            catch (Exception ex)
                {
                Logger.LogException(ex);
                }
            }


        private ExtendedDataTable GetCadTable(string device_id = "%", string cad_id = "%", string bus_id = "%")
            {
            ExtendedDataTable dt = new ExtendedDataTable();
            //List<object> obj = new List<object>();
            try
                {
                using (MySqlConnection cn = new MySqlConnection(DbConnect.ConnectionString))
                    {
                    cn.Open();
                    //List<string> SelectList = new List<string>();
                    StringBuilder SelectList = new StringBuilder();
                    SelectList.Append("a.id as cad_number_id,");
                    SelectList.Append("cad_number as cad_number_cad_number,");
                    SelectList.Append("a.agency_id as cad_number_agency_id,");
                    SelectList.Append("pcr_demographic_id as cad_number_pcr_demographic_id,");
                    SelectList.Append("pcr_disposition_id as cad_number_pcr_disposition_id,");
                    SelectList.Append("call_intake_id as cad_number_call_intake_id,");
                    SelectList.Append("pcr_id as cad_number_pcr_id,");
                    SelectList.Append("bus_id as cad_number_bus_id,");
                    SelectList.Append("date as cad_number_date,");
                    SelectList.Append("schedule_return as cad_number_schedule_return,");
                    SelectList.Append("downloaded as cad_number_downloaded,");
                    SelectList.Append("downloaded_time as cad_number_downloaded_time,");
                    SelectList.Append("cancelled as cad_number_cancelled,");
                    SelectList.Append("firstCrewMember as cad_number_firstCrewMember,");
                    SelectList.Append("secondCrewMember as cad_number_secondCrewMember,");
                    SelectList.Append("is_schedule_return as cad_number_is_schedule_return,");
                    SelectList.Append("is_cancelled as cad_number_is_cancelled,");
                    SelectList.Append("a.utc_insert as cad_number_utc_insert,");
                    SelectList.Append("a.utc_update as cad_number_utc_update,");
                    SelectList.Append("user_login_id as cad_number_user_login_id,");
                    SelectList.Append("schedule_cad_id as cad_number_schedule_cad_id,");
                    SelectList.Append("caller_name as cad_number_caller_name,");
                    SelectList.Append("caller_phone as cad_number_caller_phone,");
                    SelectList.Append("transfer_care as cad_number_transfer_care,");
                    SelectList.Append("is_transfer_care as cad_number_is_transfer_care,");
                    SelectList.Append("a.active as cad_number_active,");
                    SelectList.Append("is_dry_run as cad_number_is_dry_run,");
                    SelectList.Append("first_name as pcr_demographic_first_name,");
                    SelectList.Append("last_name as pcr_demographic_last_name,");
                    SelectList.Append("e.address as address_address,");
                    SelectList.Append("city.id as city_id,");
                    SelectList.Append("city.city_name as city_city,");
                    SelectList.Append("state.id as state_id,");
                    SelectList.Append("state.state_name as state_state,");
                    SelectList.Append("zip.id as zip_id,");
                    SelectList.Append("zip.zip_code as zip_zip,");
                    SelectList.Append("country.id as country_id,");
                    SelectList.Append("country.country_name as country_country");
                    string SelectString = SelectList.ToString();     // string.Join(",", SelectList);
                    string QueryString = "SELECT " + SelectString + " FROM cad_number a " + System.Environment.NewLine +
                                             "left outer join call_intake b " + System.Environment.NewLine +
                                             "on a.call_intake_id = b.id " + System.Environment.NewLine +
                                             "left outer join pcr_demographic c " + System.Environment.NewLine +
                                             "on a.pcr_demographic_id = c.id " + System.Environment.NewLine +
                                             "left outer join person d " + System.Environment.NewLine +
                                             "on c.pt_person = d.id " + System.Environment.NewLine +
                                             "left outer join address e " + System.Environment.NewLine +
                                             "on e.id = ifnull(b.facility_id, b.address_id) " + System.Environment.NewLine +
                                             "left outer join city on e.city_id=city.id " + System.Environment.NewLine +
                                             "left outer join state on e.state_id=state.id " + System.Environment.NewLine +
                                             "left outer join zip on e.zip_id=zip.id " + System.Environment.NewLine +
                                             "left outer join country on e.country_id=country.id " + System.Environment.NewLine +
                                             "where a.id like '" + cad_id + "' and a.agency_id like '" + device_id + "'" + " and a.bus_id like '" + bus_id + "'" +
                                             " and (ifnull(downloaded_time,0)=0 or year(downloaded_time)=1970)";
                    //where a.id = '70d9fafb-64da-4eb3-b3d8-f99950952474' and and a.agency_id = '90b16e1c-aa76-11e5-b94a-842b2b4bbc99';
                    MySqlCommand cmd = new MySqlCommand(QueryString, cn);

                    dt.Load(cmd.ExecuteReader());

                    }
                }
            catch (Exception ex) { Logger.LogException(ex); return null; }
            return dt;

            }

        

          [HttpPost]
        public Boolean PostPcrFromIOS([FromBody] object JsonData)
          {
             try
                {
                    Pcr pcr = new Pcr();
                    pcr.MapFromIOSJson(JsonData);
                    return true;
                }
            catch (Exception ex) { Logger.LogException(ex); return false; }

            }
         [HttpGet]
          public Boolean GetJsonFromEMS(string pcr_id)
          {
               var sigToImg = new ConsumedByCode.SignatureToImage.SignatureToImage();
               var signatureImage = sigToImg.SigJsonToImage("[{\"lx\":170,\"ly\":16,\"mx\":170,\"my\":15},{\"lx\":169,\"ly\":20,\"mx\":169,\"my\":19},{\"lx\":170,\"ly\":20,\"mx\":169,\"my\":20},{\"lx\":177,\"ly\":26,\"mx\":170,\"my\":20},{\"lx\":186,\"ly\":32,\"mx\":177,\"my\":26},{\"lx\":195,\"ly\":39,\"mx\":186,\"my\":32},{\"lx\":199,\"ly\":43,\"mx\":195,\"my\":39},{\"lx\":203,\"ly\":46,\"mx\":199,\"my\":43},{\"lx\":206,\"ly\":50,\"mx\":203,\"my\":46},{\"lx\":207,\"ly\":51,\"mx\":206,\"my\":50},{\"lx\":208,\"ly\":52,\"mx\":207,\"my\":51},{\"lx\":173,\"ly\":16,\"mx\":173,\"my\":15},{\"lx\":175,\"ly\":13,\"mx\":173,\"my\":16},{\"lx\":179,\"ly\":11,\"mx\":175,\"my\":13},{\"lx\":182,\"ly\":10,\"mx\":179,\"my\":11},{\"lx\":185,\"ly\":8,\"mx\":182,\"my\":10},{\"lx\":188,\"ly\":8,\"mx\":185,\"my\":8},{\"lx\":192,\"ly\":8,\"mx\":188,\"my\":8},{\"lx\":195,\"ly\":8,\"mx\":192,\"my\":8},{\"lx\":196,\"ly\":8,\"mx\":195,\"my\":8},{\"lx\":197,\"ly\":8,\"mx\":196,\"my\":8},{\"lx\":227,\"ly\":27,\"mx\":227,\"my\":26},{\"lx\":231,\"ly\":26,\"mx\":227,\"my\":27},{\"lx\":236,\"ly\":26,\"mx\":231,\"my\":26},{\"lx\":244,\"ly\":26,\"mx\":236,\"my\":26},{\"lx\":250,\"ly\":25,\"mx\":244,\"my\":26},{\"lx\":253,\"ly\":24,\"mx\":250,\"my\":25},{\"lx\":256,\"ly\":22,\"mx\":253,\"my\":24},{\"lx\":260,\"ly\":20,\"mx\":256,\"my\":22},{\"lx\":266,\"ly\":17,\"mx\":260,\"my\":20},{\"lx\":272,\"ly\":12,\"mx\":266,\"my\":17},{\"lx\":281,\"ly\":5,\"mx\":272,\"my\":12},{\"lx\":284,\"ly\":4,\"mx\":281,\"my\":5},{\"lx\":287,\"ly\":2,\"mx\":284,\"my\":4},{\"lx\":288,\"ly\":2,\"mx\":287,\"my\":2},{\"lx\":267,\"ly\":24,\"mx\":267,\"my\":23},{\"lx\":267,\"ly\":20,\"mx\":267,\"my\":24},{\"lx\":267,\"ly\":18,\"mx\":267,\"my\":20},{\"lx\":267,\"ly\":16,\"mx\":267,\"my\":18},{\"lx\":267,\"ly\":15,\"mx\":267,\"my\":16},{\"lx\":268,\"ly\":15,\"mx\":267,\"my\":15},{\"lx\":270,\"ly\":15,\"mx\":268,\"my\":15},{\"lx\":278,\"ly\":16,\"mx\":270,\"my\":15},{\"lx\":286,\"ly\":17,\"mx\":278,\"my\":16},{\"lx\":294,\"ly\":18,\"mx\":286,\"my\":17},{\"lx\":303,\"ly\":18,\"mx\":294,\"my\":18},{\"lx\":306,\"ly\":18,\"mx\":303,\"my\":18},{\"lx\":307,\"ly\":18,\"mx\":306,\"my\":18},{\"lx\":308,\"ly\":18,\"mx\":307,\"my\":18},{\"lx\":291,\"ly\":15,\"mx\":291,\"my\":14},{\"lx\":291,\"ly\":16,\"mx\":291,\"my\":15},{\"lx\":291,\"ly\":17,\"mx\":291,\"my\":16},{\"lx\":292,\"ly\":17,\"mx\":291,\"my\":17},{\"lx\":294,\"ly\":17,\"mx\":292,\"my\":17},{\"lx\":296,\"ly\":18,\"mx\":294,\"my\":17},{\"lx\":297,\"ly\":18,\"mx\":296,\"my\":18},{\"lx\":299,\"ly\":18,\"mx\":297,\"my\":18},{\"lx\":300,\"ly\":18,\"mx\":299,\"my\":18},{\"lx\":301,\"ly\":18,\"mx\":300,\"my\":18},{\"lx\":301,\"ly\":17,\"mx\":301,\"my\":18},{\"lx\":301,\"ly\":15,\"mx\":301,\"my\":17},{\"lx\":299,\"ly\":15,\"mx\":301,\"my\":15},{\"lx\":298,\"ly\":15,\"mx\":299,\"my\":15},{\"lx\":296,\"ly\":16,\"mx\":298,\"my\":15},{\"lx\":296,\"ly\":15,\"mx\":296,\"my\":16},{\"lx\":296,\"ly\":14,\"mx\":296,\"my\":15},{\"lx\":296,\"ly\":13,\"mx\":296,\"my\":14},{\"lx\":296,\"ly\":12,\"mx\":296,\"my\":13},{\"lx\":296,\"ly\":10,\"mx\":296,\"my\":12},{\"lx\":296,\"ly\":11,\"mx\":296,\"my\":10},{\"lx\":296,\"ly\":13,\"mx\":296,\"my\":11},{\"lx\":296,\"ly\":14,\"mx\":296,\"my\":13},{\"lx\":296,\"ly\":15,\"mx\":296,\"my\":14},{\"lx\":296,\"ly\":16,\"mx\":296,\"my\":15},{\"lx\":296,\"ly\":17,\"mx\":296,\"my\":16},{\"lx\":297,\"ly\":18,\"mx\":296,\"my\":17},{\"lx\":297,\"ly\":19,\"mx\":297,\"my\":18},{\"lx\":297,\"ly\":18,\"mx\":297,\"my\":19},{\"lx\":299,\"ly\":18,\"mx\":297,\"my\":18},{\"lx\":300,\"ly\":18,\"mx\":299,\"my\":18},{\"lx\":301,\"ly\":18,\"mx\":300,\"my\":18},{\"lx\":303,\"ly\":18,\"mx\":301,\"my\":18},{\"lx\":304,\"ly\":18,\"mx\":303,\"my\":18},{\"lx\":305,\"ly\":18,\"mx\":304,\"my\":18},{\"lx\":305,\"ly\":17,\"mx\":305,\"my\":18},{\"lx\":305,\"ly\":16,\"mx\":305,\"my\":17},{\"lx\":305,\"ly\":14,\"mx\":305,\"my\":16},{\"lx\":304,\"ly\":13,\"mx\":305,\"my\":14},{\"lx\":303,\"ly\":13,\"mx\":304,\"my\":13},{\"lx\":300,\"ly\":13,\"mx\":303,\"my\":13},{\"lx\":298,\"ly\":13,\"mx\":300,\"my\":13},{\"lx\":297,\"ly\":13,\"mx\":298,\"my\":13},{\"lx\":298,\"ly\":14,\"mx\":297,\"my\":13},{\"lx\":300,\"ly\":15,\"mx\":298,\"my\":14},{\"lx\":301,\"ly\":15,\"mx\":300,\"my\":15},{\"lx\":302,\"ly\":15,\"mx\":301,\"my\":15},{\"lx\":302,\"ly\":16,\"mx\":302,\"my\":15},{\"lx\":302,\"ly\":17,\"mx\":302,\"my\":16},{\"lx\":302,\"ly\":18,\"mx\":302,\"my\":17},{\"lx\":302,\"ly\":20,\"mx\":302,\"my\":18},{\"lx\":302,\"ly\":22,\"mx\":302,\"my\":20},{\"lx\":302,\"ly\":25,\"mx\":302,\"my\":22},{\"lx\":302,\"ly\":28,\"mx\":302,\"my\":25},{\"lx\":302,\"ly\":29,\"mx\":302,\"my\":28},{\"lx\":302,\"ly\":30,\"mx\":302,\"my\":29},{\"lx\":303,\"ly\":30,\"mx\":302,\"my\":30},{\"lx\":304,\"ly\":29,\"mx\":303,\"my\":30},{\"lx\":305,\"ly\":27,\"mx\":304,\"my\":29},{\"lx\":307,\"ly\":26,\"mx\":305,\"my\":27},{\"lx\":308,\"ly\":26,\"mx\":307,\"my\":26},{\"lx\":308,\"ly\":24,\"mx\":308,\"my\":26},{\"lx\":308,\"ly\":23,\"mx\":308,\"my\":24},{\"lx\":308,\"ly\":22,\"mx\":308,\"my\":23},{\"lx\":308,\"ly\":21,\"mx\":308,\"my\":22},{\"lx\":307,\"ly\":21,\"mx\":308,\"my\":21},{\"lx\":306,\"ly\":21,\"mx\":307,\"my\":21},{\"lx\":305,\"ly\":21,\"mx\":306,\"my\":21},{\"lx\":304,\"ly\":21,\"mx\":305,\"my\":21},{\"lx\":305,\"ly\":21,\"mx\":304,\"my\":21},{\"lx\":306,\"ly\":21,\"mx\":305,\"my\":21},{\"lx\":307,\"ly\":21,\"mx\":306,\"my\":21},{\"lx\":309,\"ly\":21,\"mx\":307,\"my\":21},{\"lx\":309,\"ly\":20,\"mx\":309,\"my\":21},{\"lx\":309,\"ly\":19,\"mx\":309,\"my\":20},{\"lx\":309,\"ly\":18,\"mx\":309,\"my\":19},{\"lx\":309,\"ly\":17,\"mx\":309,\"my\":18},{\"lx\":310,\"ly\":16,\"mx\":309,\"my\":17},{\"lx\":315,\"ly\":14,\"mx\":315,\"my\":13},{\"lx\":315,\"ly\":19,\"mx\":315,\"my\":14},{\"lx\":316,\"ly\":27,\"mx\":315,\"my\":19},{\"lx\":319,\"ly\":41,\"mx\":316,\"my\":27},{\"lx\":321,\"ly\":55,\"mx\":319,\"my\":41},{\"lx\":323,\"ly\":69,\"mx\":321,\"my\":55},{\"lx\":310,\"ly\":15,\"mx\":310,\"my\":14},{\"lx\":327,\"ly\":12,\"mx\":327,\"my\":11},{\"lx\":328,\"ly\":10,\"mx\":327,\"my\":12},{\"lx\":329,\"ly\":9,\"mx\":328,\"my\":10},{\"lx\":331,\"ly\":9,\"mx\":329,\"my\":9},{\"lx\":332,\"ly\":9,\"mx\":331,\"my\":9},{\"lx\":334,\"ly\":9,\"mx\":332,\"my\":9},{\"lx\":337,\"ly\":9,\"mx\":334,\"my\":9},{\"lx\":319,\"ly\":36,\"mx\":319,\"my\":35},{\"lx\":320,\"ly\":35,\"mx\":319,\"my\":36}]");
               //signatureImage = sigToImg.SigJsonToImage("[{\"lx\":279,\"ly\":82,\"mx\":279,\"my\":81},{\"lx\":278,\"ly\":82,\"mx\":279,\"my\":82},{\"lx\":276,\"ly\":84,\"mx\":278,\"my\":82},{\"lx\":275,\"ly\":86,\"mx\":276,\"my\":84},{\"lx\":274,\"ly\":88,\"mx\":275,\"my\":86},{\"lx\":272,\"ly\":89,\"mx\":274,\"my\":88},{\"lx\":271,\"ly\":90,\"mx\":272,\"my\":89},{\"lx\":271,\"ly\":93,\"mx\":271,\"my\":90},{\"lx\":271,\"ly\":94,\"mx\":271,\"my\":93},{\"lx\":269,\"ly\":96,\"mx\":271,\"my\":94},{\"lx\":267,\"ly\":98,\"mx\":269,\"my\":96},{\"lx\":267,\"ly\":99,\"mx\":267,\"my\":98},{\"lx\":265,\"ly\":102,\"mx\":267,\"my\":99},{\"lx\":261,\"ly\":106,\"mx\":265,\"my\":102},{\"lx\":258,\"ly\":109,\"mx\":261,\"my\":106},{\"lx\":253,\"ly\":111,\"mx\":258,\"my\":109},{\"lx\":249,\"ly\":114,\"mx\":253,\"my\":111},{\"lx\":246,\"ly\":116,\"mx\":249,\"my\":114},{\"lx\":243,\"ly\":119,\"mx\":246,\"my\":116},{\"lx\":238,\"ly\":121,\"mx\":243,\"my\":119},{\"lx\":235,\"ly\":122,\"mx\":238,\"my\":121},{\"lx\":228,\"ly\":125,\"mx\":235,\"my\":122},{\"lx\":226,\"ly\":126,\"mx\":228,\"my\":125},{\"lx\":222,\"ly\":127,\"mx\":226,\"my\":126},{\"lx\":216,\"ly\":127,\"mx\":222,\"my\":127},{\"lx\":207,\"ly\":127,\"mx\":216,\"my\":127},{\"lx\":203,\"ly\":127,\"mx\":207,\"my\":127},{\"lx\":198,\"ly\":127,\"mx\":203,\"my\":127},{\"lx\":192,\"ly\":127,\"mx\":198,\"my\":127},{\"lx\":188,\"ly\":127,\"mx\":192,\"my\":127},{\"lx\":184,\"ly\":127,\"mx\":188,\"my\":127},{\"lx\":181,\"ly\":126,\"mx\":184,\"my\":127},{\"lx\":176,\"ly\":124,\"mx\":181,\"my\":126},{\"lx\":174,\"ly\":122,\"mx\":176,\"my\":124},{\"lx\":172,\"ly\":122,\"mx\":174,\"my\":122},{\"lx\":170,\"ly\":119,\"mx\":172,\"my\":122},{\"lx\":168,\"ly\":117,\"mx\":170,\"my\":119},{\"lx\":167,\"ly\":114,\"mx\":168,\"my\":117},{\"lx\":165,\"ly\":111,\"mx\":167,\"my\":114},{\"lx\":164,\"ly\":110,\"mx\":165,\"my\":111},{\"lx\":164,\"ly\":107,\"mx\":164,\"my\":110},{\"lx\":163,\"ly\":104,\"mx\":164,\"my\":107},{\"lx\":163,\"ly\":102,\"mx\":163,\"my\":104},{\"lx\":163,\"ly\":99,\"mx\":163,\"my\":102},{\"lx\":163,\"ly\":97,\"mx\":163,\"my\":99},{\"lx\":163,\"ly\":94,\"mx\":163,\"my\":97},{\"lx\":163,\"ly\":90,\"mx\":163,\"my\":94},{\"lx\":169,\"ly\":86,\"mx\":163,\"my\":90},{\"lx\":175,\"ly\":82,\"mx\":169,\"my\":86},{\"lx\":183,\"ly\":77,\"mx\":175,\"my\":82},{\"lx\":192,\"ly\":75,\"mx\":183,\"my\":77},{\"lx\":202,\"ly\":72,\"mx\":192,\"my\":75},{\"lx\":211,\"ly\":70,\"mx\":202,\"my\":72},{\"lx\":222,\"ly\":66,\"mx\":211,\"my\":70},{\"lx\":231,\"ly\":65,\"mx\":222,\"my\":66},{\"lx\":243,\"ly\":64,\"mx\":231,\"my\":65},{\"lx\":253,\"ly\":63,\"mx\":243,\"my\":64},{\"lx\":264,\"ly\":62,\"mx\":253,\"my\":63},{\"lx\":274,\"ly\":62,\"mx\":264,\"my\":62},{\"lx\":282,\"ly\":62,\"mx\":274,\"my\":62},{\"lx\":289,\"ly\":62,\"mx\":282,\"my\":62},{\"lx\":295,\"ly\":62,\"mx\":289,\"my\":62},{\"lx\":298,\"ly\":63,\"mx\":295,\"my\":62},{\"lx\":302,\"ly\":66,\"mx\":298,\"my\":63},{\"lx\":303,\"ly\":66,\"mx\":302,\"my\":66},{\"lx\":303,\"ly\":68,\"mx\":303,\"my\":66},{\"lx\":306,\"ly\":70,\"mx\":303,\"my\":68},{\"lx\":307,\"ly\":72,\"mx\":306,\"my\":70},{\"lx\":307,\"ly\":73,\"mx\":307,\"my\":72},{\"lx\":307,\"ly\":75,\"mx\":307,\"my\":73},{\"lx\":310,\"ly\":78,\"mx\":307,\"my\":75},{\"lx\":310,\"ly\":80,\"mx\":310,\"my\":78},{\"lx\":311,\"ly\":84,\"mx\":310,\"my\":80},{\"lx\":311,\"ly\":88,\"mx\":311,\"my\":84},{\"lx\":311,\"ly\":93,\"mx\":311,\"my\":88},{\"lx\":311,\"ly\":97,\"mx\":311,\"my\":93},{\"lx\":311,\"ly\":101,\"mx\":311,\"my\":97},{\"lx\":311,\"ly\":106,\"mx\":311,\"my\":101},{\"lx\":311,\"ly\":111,\"mx\":311,\"my\":106},{\"lx\":311,\"ly\":115,\"mx\":311,\"my\":111},{\"lx\":311,\"ly\":119,\"mx\":311,\"my\":115},{\"lx\":311,\"ly\":125,\"mx\":311,\"my\":119},{\"lx\":311,\"ly\":130,\"mx\":311,\"my\":125},{\"lx\":314,\"ly\":137,\"mx\":311,\"my\":130},{\"lx\":315,\"ly\":142,\"mx\":314,\"my\":137},{\"lx\":316,\"ly\":146,\"mx\":315,\"my\":142},{\"lx\":319,\"ly\":154,\"mx\":316,\"my\":146},{\"lx\":320,\"ly\":158,\"mx\":319,\"my\":154},{\"lx\":321,\"ly\":163,\"mx\":320,\"my\":158},{\"lx\":323,\"ly\":166,\"mx\":321,\"my\":163},{\"lx\":325,\"ly\":171,\"mx\":323,\"my\":166},{\"lx\":327,\"ly\":174,\"mx\":325,\"my\":171},{\"lx\":329,\"ly\":179,\"mx\":327,\"my\":174},{\"lx\":333,\"ly\":184,\"mx\":329,\"my\":179},{\"lx\":334,\"ly\":186,\"mx\":333,\"my\":184},{\"lx\":336,\"ly\":189,\"mx\":334,\"my\":186},{\"lx\":339,\"ly\":193,\"mx\":336,\"my\":189},{\"lx\":341,\"ly\":195,\"mx\":339,\"my\":193},{\"lx\":341,\"ly\":196,\"mx\":341,\"my\":195},{\"lx\":342,\"ly\":196,\"mx\":341,\"my\":196},{\"lx\":343,\"ly\":197,\"mx\":342,\"my\":196}]");
               signatureImage.Save("c:\\temp\\my.png", System.Drawing.Imaging.ImageFormat.Png);
               Image im = (Image)signatureImage;
               JavaScriptSerializer jss = new JavaScriptSerializer();

               try
               {
                    Pcr.OutgoingJson = "";
                    Pcr pcr = new Pcr();
                    Json_pcr jcon_pcr = new Json_pcr(pcr_id);
                    jcon_pcr.Retrieve();
                    //object JsonData = Utilities.GetIOSJson(pcr_id);
                    if (jcon_pcr.data == null) jcon_pcr.data = (new JObject()).ToString();
                    pcr.MapIntoIOSJson(jcon_pcr.data, pcr_id);
                    jcon_pcr.data = Pcr.OutgoingJson.ToString();
                    //jcon_pcr.sent = Convert.ToDateTime(DateTime.Now).ToString("yyyy-MM-dd hh:mm:ss");
                    jcon_pcr.HandleRecord();
                    //Utilities.OutputIOSJson(Pcr.OutgoingJson.ToString(), pcr_id);
                    return true;
               }
               catch (Exception ex) { Logger.LogException(ex); return false; }

          }
         [HttpGet]
          public Boolean GetJsonFromIOS(string pcr_id)
          {
               try
               {
                    Pcr pcr = new Pcr();
                    Json_pcr json_pcr = new Json_pcr(pcr_id);
                    json_pcr.Retrieve();
                    if (json_pcr.data != null && Convert.ToDateTime(json_pcr.received).Year == 1970)
                    {
                         pcr.MapFromIOSJson(json_pcr.data, pcr_id, json_pcr.agency);
                         json_pcr.received = Convert.ToDateTime(DateTime.Now).ToString("yyyy-MM-dd hh:mm:ss");
                         json_pcr.HandleRecord();
                         return true;
                    }
                    return false;
               }
               catch (Exception ex) { Logger.LogException(ex); return false; }

          }
        [HttpPost]
        public Boolean PostJsonToSql([FromBody] object JsonData)
            {
            try
                {
                JObject JsonObject = (JObject)JsonConvert.DeserializeObject(JsonData.ToString());
                string TableName;
                foreach (JToken token in JsonObject.SelectToken(""))
                    {

                    foreach (JToken Fields in token)
                        {
                        int LastIndex = 0;
                        if (Fields.Type.ToString() == "Array") LastIndex = Fields.Count() - 1;


                        for (int i = 0; i <= LastIndex; i++)
                            {
                            JToken WorkFields;
                            if (Fields.Type.ToString() == "Array")
                                WorkFields = Fields.ToArray()[i];
                            else
                                WorkFields = Fields;
                            JsonInputSection PcrSection = new JsonInputSection();
                            foreach (JToken Field in WorkFields.Children())
                                {
                                var Property = Field as JProperty;
                                PcrSection[Property.Name] = Property.Value.ToString();
                                }
                            TableName = token.Path.ToString();
                            if (TableName.Contains("."))
                                TableName = TableName.Remove(0, TableName.IndexOf(".") + 1);
                            Assembly Asm = Assembly.Load(Assembly.GetExecutingAssembly().FullName);
                            Type PCRSectionType = Asm.GetTypes().First(t => token.Path.ToUpper().EndsWith(t.Name.ToUpper()));
                            dynamic objSectionOut = Activator.CreateInstance(PCRSectionType, TableName, PcrSection);
                            objSectionOut.HandleRecord();
                            }
                        }
                    }
                Logger.LogAction(JsonObject.ToString(), "JSON_Posts");
                return true;
                }
            catch (Exception ex) { Logger.LogException(ex); return false; }

            }




        }

    }
