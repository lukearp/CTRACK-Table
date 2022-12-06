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
            ILogger log)
        {
            string requestBody = await new StreamReader(blob).ReadToEndAsync();

            JArray data = JArray.Parse(requestBody);
            List<CtrackActor> actors = new List<CtrackActor>();
            List<CtrackContacts> contacts;
            List<CtrackAddress> addresses;
            string active = "";
            ParticipantInfo entryName;
            foreach (JObject entry in data)
            {
                contacts = new List<CtrackContacts>();
                addresses = new List<CtrackAddress>();
                if (entry["ATY_STATUS"].Value<string>() == "1")
                {
                    active = "10098";
                }
                else
                {
                    active = "10099";
                }
                contacts.Add(new CtrackContacts() { contactType = "400018", contactValue = entry["ATY_PHONE"].Value<string>() });
                if(entry["ATY_EMAIL"].Value<string>() != "")
                {
                    contacts.Add(new CtrackContacts() { contactType = "23", contactValue = entry["ATY_EMAIL"].Value<string>() });
                }
                if (entry["ATY_FAX"].Value<string>() != "0000000000")
                {
                    contacts.Add(new CtrackContacts() { contactType = "24", contactValue = entry["ATY_FAX"].Value<string>() });
                }
                if (entry["ATY_ADDR_1"].Value<string>() == "" && entry["ATY_ADDR_2"].Value<string>() != "")
                {
                    addresses.Add(new CtrackAddress() { city = entry["ATY_CITY"].Value<string>(), line1 = entry["ATY_ADDR_2"].Value<string>(), line2 = "", zipCode = entry["ATY_ZIP"].Value<string>(), regionType = "1000008", });
                }
                if (entry["ATY_ADDR_1"].Value<string>() != "")
                {
                    addresses.Add(new CtrackAddress() { city = entry["ATY_CITY"].Value<string>(), line1 = entry["ATY_ADDR_1"].Value<string>(), line2 = entry["ATY_ADDR_2"].Value<string>(), zipCode = entry["ATY_ZIP"].Value<string>(), regionType = "1000008", });
                }
                entryName = ParticipantInfo.ParseName(entry["ATY_FULL_NAME"].Value<string>());
                string barAdmit = entry["ATY_DOA"].Value<DateTime>().ToString("yyyy-MM-dd");
                actors.Add(new CtrackActor() { actorTypeDetails = new CtrackActorDetails() { actorTypeID = "10000", attorneyStatus = active, barNumber = entry["ATY_BAR_ID"].Value<string>(), barAdmittedDate = barAdmit }, firstName = entryName.firstName, middleName = entryName.middleName, lastName = entryName.lastName, contacts = contacts, addresses = addresses });
            }
            CtrackBulkRequest bulkRequest = new CtrackBulkRequest();
            foreach (CtrackActor actor in actors)
            {
                bulkRequest.items.Add(new CtrackBulkRequestItem() { uri = "/v1/actors", httpMethod = "POST", requestBody = actor });
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

        public static ParticipantInfo ParseName(string name)
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
        public CtrackActor requestBody { get; set; }
        public string resultName { get; }

        public CtrackBulkRequestItem()
        {
            resultName = Guid.NewGuid().ToString().Replace("-", "");
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
        public List<CtrackContacts> contacts { get; set; }
        public List<CtrackAddress> addresses { get; set; }

        public CtrackActor()
        {
            scopeID = "1";
            actorCategoryID = "1";
            contacts = new List<CtrackContacts>();
            addresses = new List<CtrackAddress>();
        }
    }

    class CtrackActorDetails
    {
        public string actorTypeID { get; set; }
        public string actorSubTypeID { get; }
        public string attorneyStatus { get; set; }
        public string barNumber { get; set; }
        public string effectiveDate { get; set; }
        public string barAdmittedDate { get; set; }

        public CtrackActorDetails()
        {
            actorSubTypeID = "10000";
            effectiveDate = DateTime.Now.ToString("yyyy-MM-dd");
        }
    }

    class CtrackContacts
    {
        public string contactType { get; set; }
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
    }
}

