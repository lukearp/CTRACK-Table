using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.Storage.Blobs;

namespace Company.Function
{
    public static class CTRACK_Parse
    {
        [FunctionName("CTRACK_Parse")]
        [StorageAccount("CTRACK_STORAGE")]
        public static async Task<String> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Blob("test/data.json", FileAccess.Read)] Stream blob,
            [Blob("test/data-parased.json", FileAccess.Write)] Stream output,
            ILogger log)
        {
            string requestBody = await new StreamReader(blob).ReadToEndAsync();
            JArray data = JArray.Parse(requestBody);
            List<CtrackActor> actors = new List<CtrackActor>();
            CtrackBulkRequest bulkRequest = new CtrackBulkRequest();
            List<CtrackAddress> addresses;
            CtrackBulkRequestItem actorItem;
            string active = "";
            ParticipantInfo entryName;
            string phone;
            string fax;
            int count = 0;
            foreach (JObject entry in data)
            {
                addresses = new List<CtrackAddress>();                
                active = entry["ACTOR_STATUS"].Value<string>();
                phone = "+1 " + entry["PHONE"].Value<string>().Substring(0, 10);
                fax = "+1 " + entry["FAX"].Value<string>().Substring(0, 10);
                if (entry["ADDR_1"].Value<string>() == "" && entry["ADDR_2"].Value<string>() != "" && entry["US_STATE"].Value<string>() != "")
                {
                    addresses.Add(new CtrackAddress() { city = entry["CITY"].Value<string>(), line1 = entry["ADDR_2"].Value<string>(), line2 = "", zipCode = entry["ZIP"].Value<string>(), regionType = RegionIds.regions[entry["US_STATE"].Value<string>()] });
                }
                if (entry["ADDR_1"].Value<string>() != "" && entry["US_STATE"].Value<string>() != "")
                {
                    addresses.Add(new CtrackAddress() { city = entry["CITY"].Value<string>(), line1 = entry["ADDR_1"].Value<string>(), line2 = entry["ADDR_2"].Value<string>(), zipCode = entry["ZIP"].Value<string>(), regionType = RegionIds.regions[entry["US_STATE"].Value<string>()] });
                }
                if (entry["TYPEID"].Value<string>() == "10000")
                {
                    entryName = ParticipantInfo.ParseNameAttorney(entry["FULL_NAME"].Value<string>());
                }
                else
                {
                    entryName = ParticipantInfo.ParseNameJudge(entry["FULL_NAME"].Value<string>());
                }
                string barAdmit = entry["ADMIT_DATE"].Value<DateTime>().ToString("yyyy-MM-dd");
                actors.Add(new CtrackActor() { actorTypeDetails = new CtrackActorDetails() { actorTypeID = entry["TYPEID"].Value<string>(), actorSubTypeID = "10000" ,attorneyStatus = active, barNumber = entry["BAR_ID"].Value<string>(), barAdmittedDate = barAdmit, actorTypeName = entry["TYPEID"].Value<string>() == "10000" ? "Attorney" : "Judge" }, firstName = entryName.firstName, middleName = entryName.middleName, lastName = entryName.lastName, addresses = addresses });
                if (entry["ActorID"].Value<string>() == "-1")
                {
                    actorItem = new CtrackBulkRequestItem() { uri = "/v1/actors/", httpMethod = "POST", requestBody = JObject.Parse((new CtrackActor() { actorTypeDetails = new CtrackActorDetails() { actorTypeID = entry["TYPEID"].Value<string>(), attorneyStatus = active, barNumber = entry["BAR_ID"].Value<string>(), barAdmittedDate = barAdmit }, firstName = entryName.firstName, middleName = entryName.middleName, lastName = entryName.lastName, addresses = addresses }).ToString()), resultName = "requestActor" + count };
                }
                else
                {
                    actorItem = new CtrackBulkRequestItem() { uri = ("/v1/actors/" + entry["ActorID"].Value<string>()), httpMethod = "PUT", requestBody = JObject.Parse((new CtrackActor() { actorTypeDetails = new CtrackActorDetails() { actorTypeID = entry["TYPEID"].Value<string>(), attorneyStatus = active, barNumber = entry["BAR_ID"].Value<string>(), barAdmittedDate = barAdmit }, firstName = entryName.firstName, middleName = entryName.middleName, lastName = entryName.lastName, addresses = addresses }).ToString()), resultName = "requestActor" + count };
                }
                bulkRequest.items.Add(actorItem);
                if (phone != "+1 0000000000")
                {
                    bulkRequest.items.Add(new CtrackBulkRequestItem() { uri = "/v1/actors/${" + actorItem.resultName + "}/contacts", httpMethod = "POST", requestBody = JObject.Parse((new CtrackContacts() { contactTypeEntityID = "400018", contactValue = phone }).ToString()), resultName = "ContactNum" + count });
                    // contacts.Add(new CtrackContacts() { contactTypeEntityID = "400018", contactValue = phone });
                }
                if (entry["EMAIL"].Value<string>() != "")
                {
                    bulkRequest.items.Add(new CtrackBulkRequestItem() { uri = "/v1/actors/${" + actorItem.resultName + "}/contacts", httpMethod = "POST", requestBody = JObject.Parse((new CtrackContacts() { contactTypeEntityID = "23", contactValue = entry["EMAIL"].Value<string>() }).ToString()), resultName = "requestEmail" + count });
                    //contacts.Add(new CtrackContacts() { contactTypeEntityID = "23", contactValue = entry["EMAIL"].Value<string>() });
                }
                if (fax != "+1 0000000000" && entry["TYPEID"].Value<string>() == "10000")
                {
                    bulkRequest.items.Add(new CtrackBulkRequestItem() { uri = "/v1/actors/${" + actorItem.resultName + "}/contacts", httpMethod = "POST", requestBody = JObject.Parse((new CtrackContacts() { contactTypeEntityID = "24", contactValue = fax }).ToString()), resultName = "requestFax" + count });
                    //contacts.Add(new CtrackContacts() { contactTypeEntityID = "24", contactValue = fax });
                }
                count++;
            }
            string returnValue = "--712975333129587337408149\r\nContent-Disposition: form-data; name=\"bulk\"\r\n\r\n" + bulkRequest.ToString() + "\r\n--712975333129587337408149--";
            return returnValue.ToString();
        }
    }

    class ParticipantInfo
    {
        public string prefix { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string suffix { get; set; }

        public ParticipantInfo()
        {

        }

        public static ParticipantInfo ParseNameAttorney(string name)
        {
            int count = name.Split(' ').ToArray().Count();
            string given = "";
            string sur = "";
            string middle = "";
            string suffixLocal = "";
            string prefix = "";
            string suffixPattern = @"^((?:[JS]r\.?|III?|IV|V|VI.*|I.*X|X))?$";
            string prefixPattern = @"";
            if (name != "")
            {
                switch (count)
                {
                    case 2:
                        {
                            sur = name.Split(' ')[0];
                            given = name.Split(' ')[1];
                            break;
                        }
                    case 3:
                        {
                            if (Regex.IsMatch(name.Split(' ')[2], suffixPattern))
                            {
                                sur = name.Split(' ')[0];
                                given = name.Split(' ')[1];
                                suffixLocal = name.Split(' ')[2];
                            }
                            else
                            {
                                sur = name.Split(' ')[0];
                                given = name.Split(' ')[1];
                                middle = name.Split(' ')[2];
                            }
                            break;
                        }
                    case 4:
                        {
                            if (Regex.IsMatch(name.Split(' ')[3], suffixPattern))
                            {
                                sur = name.Split(' ')[0];
                                given = name.Split(' ')[1];
                                middle = name.Split(' ')[2];
                                suffixLocal = name.Split(' ')[3];
                            }
                            else
                            {
                                sur = name.Split(' ')[0];
                                given = name.Split(' ')[1];
                                middle = name.Split(' ')[2];
                            }
                            break;
                        }
                    default:
                        {
                            sur = name.Split(' ')[0];
                            given = name.Split(' ')[1];
                            middle = name.Split(' ')[2];
                            break;
                        }
                }
            }
            return new ParticipantInfo() { firstName = given, lastName = sur, middleName = middle, suffix = suffixLocal };
        }

        public static ParticipantInfo ParseNameJudge(string name)
        {
            int count = name.Split(' ').ToArray().Count();
            string given = "";
            string sur = "";
            string middle = "";
            string suffixLocal = "";
            string prefix = "";
            string suffixPattern = @"^((?:[JS]r\.?|III?|IV|V|VI.*|I.*X|X))?$";
            string prefixPattern = @"";
            if (name != "")
            {
                switch (count)
                {
                    case 2:
                        {
                            sur = name.Split(' ')[1];
                            given = name.Split(' ')[0];
                            break;
                        }
                    case 3:
                        {
                            if (Regex.IsMatch(name.Split(' ')[2], suffixPattern))
                            {
                                sur = name.Split(' ')[1];
                                given = name.Split(' ')[0];
                                suffixLocal = name.Split(' ')[2];
                            }
                            else
                            {
                                sur = name.Split(' ')[2];
                                given = name.Split(' ')[0];
                                middle = name.Split(' ')[1];
                            }
                            break;
                        }
                    case 4:
                        {
                            if (Regex.IsMatch(name.Split(' ')[3], suffixPattern))
                            {
                                sur = name.Split(' ')[2];
                                given = name.Split(' ')[0];
                                middle = name.Split(' ')[1];
                                suffixLocal = name.Split(' ')[3];
                            }
                            else
                            {
                                sur = name.Split(' ')[2];
                                given = name.Split(' ')[0];
                                middle = name.Split(' ')[1];
                            }
                            break;
                        }
                    default:
                        {
                            sur = name.Split(' ')[2];
                            given = name.Split(' ')[0];
                            middle = name.Split(' ')[1];
                            break;
                        }
                }
            }
            return new ParticipantInfo() { firstName = given, lastName = sur, middleName = middle, suffix = suffixLocal };
        }

        /*public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }*/
    }

    class CtrackBulkRequest
    {
        public List<CtrackBulkRequestItem> items { get; set; }
        public CtrackBulkRequest()
        {
            items = new List<CtrackBulkRequestItem>();
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    class CtrackBulkRequestItem
    {
        public string uri { get; set; }
        public string httpMethod { get; set; }
        public JObject requestBody { get; set; }
        public string resultName { get; set; }

        public CtrackBulkRequestItem()
        {
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    class CtrackActor
    {
        public string actorCategoryID { get; }
        public CtrackActorDetails actorTypeDetails { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string scopeID { get; }
        //public List<CtrackContacts> contacts { get; set; }
        public List<CtrackAddress> addresses { get; set; }

        public CtrackActor()
        {
            scopeID = "1";
            actorCategoryID = "1";
            //contacts = new List<CtrackContacts>();
            addresses = new List<CtrackAddress>();
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    class CtrackActorDetails
    {
        public string actorTypeID { get; set; }
        public string actorSubTypeID { get; set;}
        public string attorneyStatus { get; set; }
        public string barNumber { get; set; }
        public string actorTypeName { get; set; }
        public string effectiveDate { get; set; }
        public string barAdmittedDate { get; set; }

        public CtrackActorDetails()
        {
            effectiveDate = DateTime.Now.ToString("yyyy-MM-dd");
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    class CtrackContacts
    {
        public string contactTypeEntityID { get; set; }
        public string contactValue { get; set; }
        public string scopeID { get; }
        public bool security1 { get; }
        public bool security2 { get; }
        public bool security3 { get; }
        public bool security4 { get; }
        public bool security5 { get; }

        public CtrackContacts()
        {
            scopeID = "1";
            security1 = security2 = security3 = security4 = security5 = false;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    class CtrackAddress
    {
        public string addressType { get; }
        public string city { get; set; }
        public string line1 { get; set; }
        public string line2 { get; set; }
        public string regionType { get; set; }
        public string country { get; }
        public string scopeID { get; }
        public string zipCode { get; set; }
        public bool security1 { get; }
        public bool security2 { get; }
        public bool security3 { get; }
        public bool security4 { get; }
        public bool security5 { get; }

        public CtrackAddress()
        {
            country = "561";
            scopeID = "1";
            security1 = security2 = security3 = security4 = security5 = false;
            addressType = "21";
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public static class RegionIds
    {
        public static Dictionary<string, string> regions = new Dictionary<string, string>() {
            {"AA","1000000"},
            {"AE","1000001"},
            {"AP","1000002"},
            {"AS","1000003"},
            {"FM","1000004"},
            {"MH","1000005"},
            {"MP","1000006"},
            {"AK","1000007"},
            {"AL","1000008"},
            {"AR","1000009"},
            {"AZ","1000010"},
            {"CA","1000011"},
            {"CO","1000012"},
            {"CT","1000013"},
            {"CZ","1000014"},
            {"DC","1000015"},
            {"DE","1000016"},
            {"FL","1000017"},
            {"GA","1000018"},
            {"GU","1000019"},
            {"HI","1000020"},
            {"IA","1000021"},
            {"ID","1000022"},
            {"IL","1000023"},
            {"IN","1000024"},
            {"KS","1000025"},
            {"KY","1000026"},
            {"LA","1000027"},
            {"MA","1000028"},
            {"MD","1000029"},
            {"ME","1000030"},
            {"MI","1000031"},
            {"MN","1000032"},
            {"MO","1000033"},
            {"MS","1000034"},
            {"MT","1000035"},
            {"NC","1000037"},
            {"ND","1000038"},
            {"NE","1000039"},
            {"NH","1000040"},
            {"NJ","1000041"},
            {"NM","1000042"},
            {"NV","1000043"},
            {"NY","1000044"},
            {"OH","1000045"},
            {"OK","1000046"},
            {"OR","1000047"},
            {"OT","1000048"},
            {"PA","1000049"},
            {"PR","1000050"},
            {"RI","1000051"},
            {"SC","1000052"},
            {"SD","1000053"},
            {"TN","1000054"},
            {"TX","1000055"},
            {"US","1000056"},
            {"UT","1000057"},
            {"VA","1000058"},
            {"VI","1000059"},
            {"VT","1000060"},
            {"WA","1000061"},
            {"WI","1000062"},
            {"WV","1000063"},
            {"WY","1000064"}
        };
    }
}

