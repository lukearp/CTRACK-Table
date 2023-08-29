using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Luke.Function
{
    public static class CTRACK_Soap
    {
        [FunctionName("CTRACK_Soap")]
        [StorageAccount("CTRACK_STORAGE")]
        public static async Task<String> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Blob("test/efile.json", FileAccess.Read)] Stream blob,
            //[Blob("test/efile_soap.xml", FileAccess.Write)] Stream output,
            ILogger log)
        {
            string requestBody = await new StreamReader(blob).ReadToEndAsync();
            JArray data = JArray.Parse(requestBody);
            List<string> filings = new List<string>();
            List<ParticipantInfo> participants;
            foreach (JObject filing in data)
            {
                participants = new List<ParticipantInfo>();
                participants.Add(new ParticipantInfo(filing["Defendant"].Value<string>(), false, false) { streetAddress = filing["Defendant_ADDR_1"].Value<string>(), city = filing["Defendant_City"].Value<string>(), state = filing["Defendant_State"].Value<string>(), zipCode = filing["Defendant_Zip"].Value<string>(), phone = filing["Defendant_Phone"].Value<string>(), country = "US", attorneyBar = filing["ATT_1_BAR"].Value<string>() });
                if (filing["ATT_1_Name"].Value<string>() != "")
                {
                    participants.Add(new ParticipantInfo(filing["ATT_1_Name"].Value<string>(), true, false) { streetAddress = filing["ATT_1_ADDR_1"].Value<string>(), city = filing["ATT_1_CITY"].Value<string>(), state = filing["ATT_1_STATE"].Value<string>(), zipCode = filing["ATT_1_ZIP"].Value<string>(), phone = filing["ATT_1_PHONE_NUMBER"].Value<string>(), country = "US", attorneyBar = filing["ATT_1_BAR"].Value<string>(), email = filing["ATT_1_EMAIL"].Value<string>() });
                }
                if (filing["ATT_2_Name"].Value<string>() != "")
                {
                    participants.Add(new ParticipantInfo(filing["ATT_2_Name"].Value<string>(), true, false) { streetAddress = filing["ATT_2_ADDR_1"].Value<string>(), city = filing["ATT_2_CITY"].Value<string>(), state = filing["ATT_2_STATE"].Value<string>(), zipCode = filing["ATT_2_ZIP"].Value<string>(), phone = filing["ATT_2_PHONE_NUMBER"].Value<string>(), country = "US", attorneyBar = filing["ATT_2_BAR"].Value<string>(), email = filing["ATT_2_EMAIL"].Value<string>() });
                }
                if (filing["JUD_1_Name"].Value<string>() != "")
                {
                    participants.Add(new ParticipantInfo(filing["JUD_1_Name"].Value<string>(), false, true) { streetAddress = filing["JUD_ADDR_1"].Value<string>(), city = filing["JUD_CITY"].Value<string>(), state = filing["JUD_STATE"].Value<string>(), zipCode = filing["JUD_ZIP"].Value<string>(), phone = filing["JUD_PHONE_NUMBER"].Value<string>(), country = "US", attorneyBar = filing["JUD_BAR"].Value<string>(), email = filing["JUD_EMAIL"].Value<string>() });
                }
                filings.Add(TransformXML.ReturnXML(participants, CourtType.CriminalCourt));
            }
            // byte[] filingsByte = filings.SelectMany(s => Encoding.UTF8.GetBytes(s + Environment.NewLine)).ToArray();

            return String.Join(",", filings);
        }
    }
    enum CourtType
    {
        SupremeCourt = 1,
        CriminalCourt = 2,
        CivilCourt = 3
    }

    class ParsedName 
    {
        public string prefix {get;set;}
        public string firstName {get;set;}
        public string lastName {get;set;}
        public string middleName {get;set;}
        public string suffix {get;set;}
    }

    class ParticipantInfo
    {
        public string guid {get;set;}
        public string givenName {get;set;}
        public string surName {get;set;}
        public string middleName {get;set;}
        public string streetAddress {get;set;}
        public string city {get;set;}
        public string state {get;set;}
        public string country {get;set;}
        public string zipCode {get;set;}
        public string phone {get;set;}
        public string role {get;set;}
        public string email {get;set;}
        public bool attorney {get;set;}
        public bool judge {get;set;}
        public string attorneyBar {get;set;}
        public string actorInstanceId {get;set;}
        static CultureInfo titleCase = new CultureInfo("en-US");

        public ParticipantInfo(string name, bool attorney, bool judge)
        {
            this.guid = Guid.NewGuid().ToString();
            ParsedName parsedName;
            this.attorney = attorney;
            this.judge = judge;
            if(judge) {
                parsedName = ParseNameJudge(name);
            }
            else {
                parsedName = ParseNameAttorney(name);
            }
            this.givenName = parsedName.firstName;
            this.middleName = parsedName.middleName;
            this.surName = parsedName.lastName;
        }

        public static ParsedName ParseNameAttorney(string name)
        {
            int count = name.Split(' ').ToArray().Count();
            string given = "";
            string sur = "";
            string middle = "";
            string suffixLocal = "";
            string prefix = "";
            string suffixPattern = @"^((?:[JS]r\.?|III?|IV|V|VI.*|I.*X|X))?$";
            string prefixPattern = @"";
            string[] splitName = name.Split(' ');
            if (name != "")
            {
                switch (count)
                {
                    case 2:
                        {
                            sur = titleCase.TextInfo.ToTitleCase(splitName[0].ToLower());
                            given = titleCase.TextInfo.ToTitleCase(splitName[1].ToLower());
                            break;
                        }
                    case 3:
                        {
                            if (Regex.IsMatch(splitName[2], suffixPattern))
                            {
                                sur = titleCase.TextInfo.ToTitleCase(splitName[0].ToLower());
                                given = titleCase.TextInfo.ToTitleCase(splitName[1].ToLower());
                                suffixLocal = splitName[2];
                            }
                            else
                            {
                                sur = titleCase.TextInfo.ToTitleCase(splitName[0].ToLower());
                                given = titleCase.TextInfo.ToTitleCase(splitName[1].ToLower());
                                middle = titleCase.TextInfo.ToTitleCase(splitName[2].ToLower());
                            }
                            break;
                        }
                    case 4:
                        {
                            if (Regex.IsMatch(splitName[3], suffixPattern))
                            {
                                sur = titleCase.TextInfo.ToTitleCase(splitName[0].ToLower());
                                given = titleCase.TextInfo.ToTitleCase(splitName[1].ToLower());
                                middle = titleCase.TextInfo.ToTitleCase(splitName[2].ToLower());
                                suffixLocal = splitName[3];
                            }
                            else
                            {
                                sur = titleCase.TextInfo.ToTitleCase(splitName[0].ToLower());
                                given = titleCase.TextInfo.ToTitleCase(splitName[1].ToLower());
                                middle = titleCase.TextInfo.ToTitleCase(splitName[2].ToLower());
                            }
                            break;
                        }
                    default:
                        {
                                sur = titleCase.TextInfo.ToTitleCase(splitName[0].ToLower());
                                given = titleCase.TextInfo.ToTitleCase(splitName[1].ToLower());
                                middle = titleCase.TextInfo.ToTitleCase(splitName[2].ToLower());
                            break;
                        }
                }
            }
            return new ParsedName() { firstName = given, lastName = sur, middleName = middle, suffix = suffixLocal };
        }

        public static ParsedName ParseNameJudge(string name)
        {
            int count = name.Split(' ').ToArray().Count();
            string given = "";
            string sur = "";
            string middle = "";
            string suffixLocal = "";
            string prefix = "";
            string suffixPattern = @"^((?:[JS]r\.?|III?|IV|V|VI.*|I.*X|X))?$";
            string prefixPattern = @"";
            string[] splitName = name.Split(' ');
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
            return new ParsedName() { firstName = given, lastName = sur, middleName = middle, suffix = suffixLocal };
        }

        /*public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }*/
    }


    class TransformXML
    {
        static string baseXml = @"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
	<soap:Header>
		<wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"" xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"" soap:mustUnderstand=""1"">
			<wsse:UsernameToken wsu:Id=""UsernameToken-4e23e867-c26e-48b5-840d-29e7f721d74a"">
				<wsse:Username>AOCToCTRACK</wsse:Username>
				<wsse:Password Type=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText"">oOIPcknHTgs8DwzVusJMvL0BrZCBMQxs5DaS1evXT~6G</wsse:Password>
			</wsse:UsernameToken>
		</wsse:Security>
	</soap:Header>
	<soap:Body>
		<ns41:ReviewFilingRequestMessage xmlns:ns1=""http://niem.gov/niem/structures/2.0"" xmlns:ns10=""http://ctrack.thomsonreuters.com/sacramento/2.0"" xmlns:ns11=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CitationCase-4.0"" xmlns:ns12=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:DomesticCase-4.0"" xmlns:ns13=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:JuvenileCase-4.0"" xmlns:ns14=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CivilCase-4.0"" xmlns:ns15=""http://niem.gov/niem/domains/screening/2.0"" xmlns:ns16=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:AppellateCase-4.0"" xmlns:ns17=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:BankruptcyCase-4.0"" xmlns:ns18=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CaseQueryMessage-4.0"" xmlns:ns19=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CoreFilingMessage-4.0"" xmlns:ns2=""http://niem.gov/niem/niem-core/2.0"" xmlns:ns20=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CaseListQueryMessage-4.0"" xmlns:ns21=""http://ctrack.thomsonreuters.com/cms/2.0"" xmlns:ns22=""http://ctrack.thomsonreuters.com/sacramentokpf/2.0"" xmlns:ns23=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:PaymentMessage-4.0"" xmlns:ns24=""urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"" xmlns:ns25=""urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"" xmlns:ns26=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:RecordDocketingMessage-4.0"" xmlns:ns27=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FeesCalculationResponseMessage-4.0"" xmlns:ns28=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CourtPolicyResponseMessage-4.0"" xmlns:ns29=""http://www.w3.org/2000/09/xmldsig#"" xmlns:ns3=""http://niem.gov/niem/domains/jxdm/4.0"" xmlns:ns30=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:ServiceInformationResponseMessage-4.0"" xmlns:ns31=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FilingListResponseMessage-4.0"" xmlns:ns32=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:ReviewFilingCallbackMessage-4.0"" xmlns:ns33=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:PaymentReceiptMessage-4.0"" xmlns:ns34=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:AppInfo-4.0"" xmlns:ns35=""http://niem.gov/niem/appinfo/2.0"" xmlns:ns36=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FeesCalculationQueryMessage-4.0"" xmlns:ns37=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:MessageReceiptMessage-4.0"" xmlns:ns38=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CaseResponseMessage-4.0"" xmlns:ns39=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CaseListResponseMessage-4.0"" xmlns:ns4=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CommonTypes-4.0"" xmlns:ns40=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FilingStatusResponseMessage-4.0"" xmlns:ns41=""urn:oasis:names:tc:legalxml-courtfiling:wsdl:WebServicesProfile-Definitions-4.0"" xmlns:ns42=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:ServiceInformationQueryMessage-4.0"" xmlns:ns43=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:RecordDocketingCallbackMessage-4.0"" xmlns:ns44=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FilingStatusQueryMessage-4.0"" xmlns:ns45=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:DocumentQueryMessage-4.0"" xmlns:ns46=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CourtPolicyQueryMessage-4.0"" xmlns:ns47=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:DocumentResponseMessage-4.0"" xmlns:ns48=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:FilingListQueryMessage-4.0"" xmlns:ns5=""http://niem.gov/niem/ansi-nist/2.0"" xmlns:ns6=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CriminalCase-4.0"" xmlns:ns7=""http://ctrack.thomsonreuters.com/ecf/2.0"" xmlns:ns9=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:ServiceReceiptMessage-4.0"">
			<ns2:DocumentApplicationName xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentBinary xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentDescriptionText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentEffectiveDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentFileControlID xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentFiledDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentInformationCutOffDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentPostDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentReceivedDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentSequenceID xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentStatus xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentTitleText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns2:DocumentSubmitter xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			<ns7:CoreFilingMessage ns1:id=""Filing{0}"">
				<ns2:DocumentApplicationName xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentBinary xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentDescriptionText>Test case submission 1 - DKS</ns2:DocumentDescriptionText>
				<ns2:DocumentEffectiveDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentFileControlID xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentFiledDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentInformationCutOffDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentPostDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentReceivedDate xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentSequenceID xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentStatus xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentTitleText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				<ns2:DocumentSubmitter ns1:id=""EfileUser2"">
					<ns2:EntityRepresentation xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""ns4:PersonType"">
						<ns2:PersonBirthDate xsi:nil=""true""/>
						<ns2:PersonCapability xsi:nil=""true""/>
						<ns2:PersonDNA xsi:nil=""true""/>
						<ns2:PersonHeightMeasure xsi:nil=""true""/>
						<ns2:PersonLanguageEnglishIndicator xsi:nil=""true""/>
						<ns2:PersonName>
							<ns2:PersonNamePrefixText xsi:nil=""true""/>
							<ns2:PersonGivenName>Donna</ns2:PersonGivenName>
							<ns2:PersonMiddleName xsi:nil=""true""/>
							<ns2:PersonSurName>User</ns2:PersonSurName>
							<ns2:PersonNameSuffixText xsi:nil=""true""/>
							<ns2:PersonMaidenName xsi:nil=""true""/>
							<ns2:PersonFullName>AACS Test 1</ns2:PersonFullName>
						</ns2:PersonName>
						<ns2:PersonOtherIdentification>
							<ns2:IdentificationID>System Admin</ns2:IdentificationID>
							<ns2:IdentificationCategoryText>Role Name</ns2:IdentificationCategoryText>
							<ns2:IdentificationSourceText xsi:nil=""true""/>
						</ns2:PersonOtherIdentification>
						<ns2:PersonPrimaryLanguage xsi:nil=""true""/>
						<ns2:PersonStateIdentification xsi:nil=""true""/>
						<ns2:PersonTaxIdentification xsi:nil=""true""/>
						<ns2:PersonWeightMeasure xsi:nil=""true""/>
						<ns7:PersonAugmentation>
							<ns7:CtrackPersonID xsi:nil=""true""/>
							<ns7:CustomFields>
								<ns7:CustomFieldsEntry>
									<entryKey>AACSTest2</entryKey>
									<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xsi:type=""xs:string"">AACS1</entryValue>
								</ns7:CustomFieldsEntry>
							</ns7:CustomFields>
						</ns7:PersonAugmentation>
					</ns2:EntityRepresentation>
				</ns2:DocumentSubmitter>
				<ns4:SendingMDELocationID ns1:id=""Court{6}"">
					<ns2:IdentificationID>AOC</ns2:IdentificationID>
					<ns2:IdentificationJurisdiction xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""ns2:TextType"">Court of Appeals</ns2:IdentificationJurisdiction>
					<ns2:IdentificationSourceText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
				</ns4:SendingMDELocationID>
				<ns4:SendingMDEProfileCode>urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:WebServicesMessaging-2.0</ns4:SendingMDEProfileCode>
				<ns6:CriminalCase>
					<ns2:ActivityIdentification xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
					<ns2:ActivityDescriptionText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
					<ns2:ActivityStatus xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
					<ns2:CaseTitleText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
					<ns2:CaseCategoryText>1000108</ns2:CaseCategoryText>
					<ns2:CaseTrackingID/>
					<ns2:CaseDocketID xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
					<ns3:CaseAugmentation>
						<ns3:CaseCourt ns1:id=""Court{6}"">
							<ns2:OrganizationIdentification xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
							<ns2:OrganizationLocation xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
							<ns2:OrganizationName xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
							<ns2:OrganizationPrimaryContactInformation xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
							<ns2:OrganizationSubUnitName xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
							<ns2:OrganizationTaxIdentification xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
							<ns2:OrganizationUnitName xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
							<ns3:CourtName xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
						</ns3:CaseCourt>
						<ns3:CaseJudge xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
					</ns3:CaseAugmentation>
                    <ns7:CaseAugmentation>
                        {1}
						{2}						
						<ns7:CtrackCaseID xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
						<ns7:CustomFields>
							<ns7:CustomFieldsEntry>
								<entryKey>caseCategoryName</entryKey>
								<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">Cival</entryValue>
							</ns7:CustomFieldsEntry>							
						</ns7:CustomFields>
						<ns7:MatterNumber xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
					</ns7:CaseAugmentation>
				</ns6:CriminalCase>
				<ns19:FilingConfidentialityIndicator>false</ns19:FilingConfidentialityIndicator>
				<ns19:FilingLeadDocument xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" ns1:id=""DocumentLink3"" xsi:type=""ns7:CtrackDocumentType"">
					<ns2:DocumentApplicationName xsi:nil=""true""/>
					<ns2:DocumentDescriptionText>This is a lead document</ns2:DocumentDescriptionText>
					<ns2:DocumentEffectiveDate xsi:nil=""true""/>
					<ns2:DocumentFileControlID xsi:nil=""true""/>
					<ns2:DocumentFiledDate>
						<ns2:DateTime>2022-06-14T16:26:00.000-04:00</ns2:DateTime>
					</ns2:DocumentFiledDate>					
					<ns2:DocumentInformationCutOffDate xsi:nil=""true""/>
					<ns2:DocumentPostDate xsi:nil=""true""/>
					<ns2:DocumentReceivedDate xsi:nil=""true""/>
					<ns2:DocumentSequenceID xsi:nil=""true""/>
					<ns2:DocumentStatus xsi:nil=""true""/>
					<ns2:DocumentTitleText>Brief - Brief Filed</ns2:DocumentTitleText>
					<ns2:DocumentSubmitter xsi:nil=""true""/>
					<ns4:DocumentMetadata>
						<ns3:RegisterActionDescriptionText xsi:nil=""true""/>
						{7}
					</ns4:DocumentMetadata>
					<ns7:Confidential>false</ns7:Confidential>
					<ns7:ExcludeFromService>false</ns7:ExcludeFromService>
					<ns7:CustomFields>
						<ns7:CustomFieldsEntry>
							<entryKey>confidential</entryKey>
							<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xsi:type=""xs:boolean"">false</entryValue>
						</ns7:CustomFieldsEntry>
						<ns7:CustomFieldsEntry>
							<entryKey>excludeFromService</entryKey>
							<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xsi:type=""xs:boolean"">false</entryValue>
						</ns7:CustomFieldsEntry>
					</ns7:CustomFields>
				</ns19:FilingLeadDocument>
				<ns19:FilingConnectedDocument xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" ns1:id=""DocumentLink4"" xsi:type=""ns7:CtrackDocumentType"">
					<ns2:DocumentApplicationName xsi:nil=""true""/>
					<ns2:DocumentDescriptionText>This is a connected document that is flagged as 'exclude from service' and 'confidential' (with confidential reasons)</ns2:DocumentDescriptionText>
					<ns2:DocumentEffectiveDate xsi:nil=""true""/>
					<ns2:DocumentFileControlID xsi:nil=""true""/>
					<ns2:DocumentFiledDate>
						<ns2:DateTime>2022-06-14T16:26:00-04:00</ns2:DateTime>
					</ns2:DocumentFiledDate>
					<ns2:DocumentInformationCutOffDate xsi:nil=""true""/>
					<ns2:DocumentPostDate xsi:nil=""true""/>
					<ns2:DocumentReceivedDate xsi:nil=""true""/>
					<ns2:DocumentSequenceID xsi:nil=""true""/>
					<ns2:DocumentStatus xsi:nil=""true""/>
					<ns2:DocumentTitleText>Appendix</ns2:DocumentTitleText>
					<ns2:DocumentSubmitter xsi:nil=""true""/>
					<ns7:Confidential>false</ns7:Confidential>
					<ns7:ExcludeFromService>false</ns7:ExcludeFromService>
				</ns19:FilingConnectedDocument>
				<ns7:BatchID>{3}</ns7:BatchID>
				<ns7:FilingSubTypeID>1000428</ns7:FilingSubTypeID>
				<ns7:CustomFields>
					<ns7:CustomFieldsEntry>
						<entryKey>confidential</entryKey>
						<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:boolean"">true</entryValue>
					</ns7:CustomFieldsEntry>
					<ns7:CustomFieldsEntry>
						<entryKey>submittedDate</entryKey>
						<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:dateTime"">{4}</entryValue>
					</ns7:CustomFieldsEntry>
					<ns7:CustomFieldsEntry>
						<entryKey>submittedNumber</entryKey>
						<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">{5}</entryValue>
					</ns7:CustomFieldsEntry>
					<ns7:CustomFieldsEntry>
						<entryKey>batchFeeTotal</entryKey>
						<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:decimal"">0.0000</entryValue>
					</ns7:CustomFieldsEntry>
					<ns7:CustomFieldsEntry>
						<entryKey>filingFeeTotal</entryKey>
						<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:decimal"">0</entryValue>
					</ns7:CustomFieldsEntry>
					<ns7:CustomFieldsEntry>
						<entryKey>emergencyFiling</entryKey>
						<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:boolean"">true</entryValue>
					</ns7:CustomFieldsEntry>
					<ns7:CustomFieldsEntry>
						<entryKey>amendedFiling</entryKey>
						<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:boolean"">true</entryValue>
					</ns7:CustomFieldsEntry>
					<ns7:CustomFieldsEntry>
						<entryKey>filingCount</entryKey>
						<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:int"">1</entryValue>
					</ns7:CustomFieldsEntry>
					<ns7:CustomFieldsEntry>
						<entryKey>importedBatchFlag</entryKey>
						<entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:boolean"">false</entryValue>
					</ns7:CustomFieldsEntry>
				</ns7:CustomFields>
			</ns7:CoreFilingMessage>
			<ns7:PaymentMessage>
				<ns7:CustomFields xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true""/>
			</ns7:PaymentMessage>
		</ns41:ReviewFilingRequestMessage>
	</soap:Body>
</soap:Envelope>";

        public static string ReturnXML(List<ParticipantInfo> participants, CourtType CourtType)
        {
            List<string> attorneys = new List<string>();
            List<string> caseparticipants = new List<string>();
            List<string> filingParties = new List<string>();
            int count = 0;
            foreach (ParticipantInfo participant in participants)
            {
                caseparticipants.Add(CaseParticipant.ReturnXML(participant.guid,
                participant.givenName,
                participant.middleName,
                participant.surName,
                participant.streetAddress,
                participant.city,
                participant.state,
                participant.country,
                participant.zipCode,
                participant.phone,
                participant.role,
                participant.email,
                participant.attorney,
                count,
                participant.actorInstanceId));
                count++;
            }
            foreach (ParticipantInfo attorney in participants.Where(x => x.attorney == true))
            {
                foreach (ParticipantInfo client in participants.Where(x => x.attorney == false && x.attorneyBar == attorney.attorneyBar))
                {
                    attorneys.Add(CaseOfficial.ReturnXML(attorney.guid, attorney.attorneyBar, client.guid));
                    filingParties.Add(FilingParty.ReturnXML(client.guid));
                }
            }
            string date = DateTime.Now.ToString("s");
            string baseId = date.Replace("-", "").Replace(":", "").Replace("T", "");
            //string batch = Guid.NewGuid().ToString().Replace("-",""); 
            return String.Format(baseXml,
            baseId,
            string.Join("", attorneys.ToArray()),
            string.Join("", caseparticipants.ToArray()),
            baseId,
            date,
            baseId,
            (int)CourtType,
            string.Join("", filingParties.ToArray()));
        }
    }

    class CaseOfficial
    {
        static string baseXml = @"<ns4:CaseOfficial>
    <ns2:RoleOfPersonReference ns1:ref=""NewParticipant{0}"" />
    <ns3:JudicialOfficialBarMembership>
        <ns3:JudicialOfficialBarIdentification>
            <ns2:IdentificationID>{1}</ns2:IdentificationID>
            <ns2:IdentificationSourceText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
        </ns3:JudicialOfficialBarIdentification>
    </ns3:JudicialOfficialBarMembership>
    <ns3:JudicialOfficialFirm xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
    <ns3:CaseOfficialCaseIdentification xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
    <ns3:CaseOfficialRoleText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
    <ns4:CaseRepresentedPartyReference ns1:ref=""NewParticipant{2}"" />
</ns4:CaseOfficial>";

        public static string ReturnXML(string refId, string bar, string clientId)
        {
            return String.Format(baseXml, refId, bar, clientId);
        }
    }

    class CaseParticipant
    {
        static string baseXml = @"<ns7:CaseParticipant ns1:id=""NewParticipant{0}"">
    <ns2:EntityRepresentation xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""ns4:PersonType"">
        <ns2:PersonBirthDate xsi:nil=""true"" />
        <ns2:PersonCapability xsi:nil=""true"" />
        <ns2:PersonDNA xsi:nil=""true"" />
        <ns2:PersonHeightMeasure xsi:nil=""true"" />
        <ns2:PersonLanguageEnglishIndicator xsi:nil=""true"" />
        <ns2:PersonName>
            <ns2:PersonNamePrefixText xsi:nil=""true"" />
            <ns2:PersonGivenName>{1}</ns2:PersonGivenName>
            <ns2:PersonMiddleName>{2}</ns2:PersonMiddleName>
            <ns2:PersonSurName>{3}</ns2:PersonSurName>
            <ns2:PersonNameSuffixText xsi:nil=""true"" />
            <ns2:PersonMaidenName xsi:nil=""true"" />
            <ns2:PersonFullName>{1} {2} {3}</ns2:PersonFullName>
        </ns2:PersonName>
        <ns2:PersonPrimaryLanguage xsi:nil=""true"" />
        <ns2:PersonStateIdentification xsi:nil=""true"" />
        <ns2:PersonTaxIdentification xsi:nil=""true"" />
        <ns2:PersonWeightMeasure xsi:nil=""true"" />
    </ns2:EntityRepresentation>
    <ns4:CaseParticipantRoleCode>{10}</ns4:CaseParticipantRoleCode>
    <ns2:ContactInformation>
        <ns2:ContactMeans xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""ns2:StructuredAddressType"">
            <ns2:AddressRecipientName xsi:nil=""true"" />
            <ns2:AddressDeliveryPoint xsi:type=""ns2:StreetType"">
                <ns2:StreetFullText>{4}|||</ns2:StreetFullText>
                <ns2:StreetNumberText xsi:nil=""true"" />
                <ns2:StreetPredirectionalText xsi:nil=""true"" />
                <ns2:StreetName xsi:nil=""true"" />
                <ns2:StreetCategoryText xsi:nil=""true"" />
                <ns2:StreetPostdirectionalText xsi:nil=""true"" />
                <ns2:StreetExtensionText xsi:nil=""true"" />
            </ns2:AddressDeliveryPoint>
            <ns2:LocationCityName>{5}</ns2:LocationCityName>
            <ns2:LocationState xsi:type=""ns2:ProperNameTextType"">{6}</ns2:LocationState>
            <ns2:LocationCountry xsi:type=""ns2:ProperNameTextType"">{7}</ns2:LocationCountry>
            <ns2:LocationPostalCode>{8}</ns2:LocationPostalCode>
            <ns2:LocationPostalExtensionCode xsi:nil=""true"" />
        </ns2:ContactMeans>
        <ns2:ContactEntity xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
        <ns2:ContactEntityDescriptionText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
        <ns2:ContactInformationDescriptionText>Address</ns2:ContactInformationDescriptionText>
        <ns2:ContactResponder xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
    </ns2:ContactInformation>
    <ns2:ContactInformation>
        <ns2:ContactMeans xmlns:ns50=""http://niem.gov/niem/proxy/xsd/2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""ns50:string"">{11}</ns2:ContactMeans>
        <ns2:ContactEntity xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
        <ns2:ContactEntityDescriptionText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
        <ns2:ContactInformationDescriptionText>Primary Email</ns2:ContactInformationDescriptionText>
        <ns2:ContactResponder xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
    </ns2:ContactInformation>
    <ns2:ContactInformation>
        <ns2:ContactMeans xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""ns2:TelephoneNumberType"">
            <ns2:TelephoneNumberRepresentation xsi:type=""ns2:FullTelephoneNumberType"">
                <ns2:TelephoneNumberFullID>{9}</ns2:TelephoneNumberFullID>
                <ns2:TelephoneSuffixID xsi:nil=""true"" />
            </ns2:TelephoneNumberRepresentation>
        </ns2:ContactMeans>
        <ns2:ContactEntity xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
        <ns2:ContactEntityDescriptionText xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
        <ns2:ContactInformationDescriptionText>Work Phone</ns2:ContactInformationDescriptionText>
        <ns2:ContactResponder xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
    </ns2:ContactInformation>
    <ns7:CtrackCaseParticipantID xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:nil=""true"" />
	<ns7:CustomFields>
    {12}
	</ns7:CustomFields>
</ns7:CaseParticipant>";

        static string attorneyCustomFieldsXml = @"<ns7:CustomFieldsEntry>
        <entryKey>parentEntityID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">45</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>entityTypeName</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">Person</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>efileEntityID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">{0}</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>efileEntityAddressID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">19</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>serviceDate</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:dateTime"">2022-05-05T15:14:00-04:00</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>serviceTypeName</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">E-mail Delivery</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>serviceTypeExternalID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">1000243</entryValue>
    </ns7:CustomFieldsEntry>";

        static string attorneyLinkedCustomFieldsXml = @"<ns7:CustomFieldsEntry>
        <entryKey>parentEntityID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">45</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>entityTypeName</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">Person</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>efileEntityID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">{0}</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>efileEntityAddressID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">19</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>serviceDate</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:dateTime"">2022-05-05T15:14:00-04:00</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>serviceTypeName</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">E-mail Delivery</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>serviceTypeExternalID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">1000243</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>actorInstanceID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">{1}</entryValue>
   </ns7:CustomFieldsEntry>
   <ns7:CustomFieldsEntry>
        <entryKey>actorInstanceName</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">{2}</entryValue>
    </ns7:CustomFieldsEntry>";

        static string clientCustomFieldsXml = @"<ns7:CustomFieldsEntry>
        <entryKey>entityTypeName</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">Person</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>entityTypeName</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">Person</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>efileEntityID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">{0}</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>efileEntityAddressID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">30</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>efileEntityContactID0</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">14</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>efileEntityContactID1</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">15</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>serviceDate</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:dateTime"">2022-05-05T15:14:00-04:00</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>serviceTypeName</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:string"">Conventional</entryValue>
    </ns7:CustomFieldsEntry>
    <ns7:CustomFieldsEntry>
        <entryKey>serviceTypeExternalID</entryKey>
        <entryValue xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""xs:long"">1000242</entryValue>
    </ns7:CustomFieldsEntry>";

        public static string ReturnXML(string id, string givenName, string middleName, string surName, string streetAddress, string city, string state, string country, string zipCode, string phone, string role, string email, bool attorney, int efileEntityID, string actorInstanceId)
        {
            if (attorney == true && actorInstanceId == "")
            {
                return String.Format(baseXml, id, givenName, middleName, surName, streetAddress, city, state, country, zipCode, phone, role, email, String.Format(attorneyCustomFieldsXml, efileEntityID.ToString()));
            }
            else if (attorney == true && actorInstanceId != "")
            {
                string fullName = givenName + " " + middleName + " " + surName;
                return String.Format(baseXml, id, givenName, middleName, surName, streetAddress, city, state, country, zipCode, phone, role, email, String.Format(attorneyLinkedCustomFieldsXml, efileEntityID.ToString(), actorInstanceId, fullName));
            }
            else
            {
                return String.Format(baseXml, id, givenName, middleName, surName, streetAddress, city, state, country, zipCode, phone, role, email, String.Format(clientCustomFieldsXml, efileEntityID.ToString()));
            }
        }
    }

    class FilingParty 
    {
        static string baseXml = @"<ns4:FilingPartyID>
							<ns2:IdentificationID>NewParticipant{0}</ns2:IdentificationID>
							<ns2:IdentificationCategoryText>New Case Participant</ns2:IdentificationCategoryText>
							<ns2:IdentificationSourceText xsi:nil=""true""/>
						</ns4:FilingPartyID>";

        public static string ReturnXML(string participant)
        {
            return String.Format(baseXml, participant);
        }
    }

}
