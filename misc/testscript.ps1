$body = @"
--712975333129587337408149
Content-Disposition: form-data; name="bulk"
{
"items": [
{
"uri": "/v1/actors",
"httpMethod": "POST",
"requestBody": {
"actorCategoryID": "1",
"actorTypeDetails": {
"actorTypeID": "10000",
"actorSubTypeID": "10000",
"attorneyStatus": "10098",
"barNumber": "1234A12B",
"effectiveDate": "2022-11-21",
"barAdmittedDate": "2022-08-18"
},
"firstName": "Luke",
"middleName": "Arp",
"lastName": "Russell",
"scopeID": "1",
"contacts": [
{
"contactType": "400018",
"contactValue": "+1 4235556457",
"scopeID": "1",
"security1": false,
"security2": false,
"security3": false,
"security4": false,
"security5": false
},
{
"contactType": "23",
"contactValue": luke@lukesprojects.com,
"scopeID": "1",
"security1": false,
"security2": false,
"security3": false,
"security4": false,
"security5": false
},
{
"contactType": "24",
"contactValue": "+1 8564445541",
"scopeID": "1",
"security1": false,
"security2": false,
"security3": false,
"security4": false,
"security5": false
}
],
"addresses": [
{
"addressType": "21",
"city": "Athens",
"line1": "124 My St.",
"line2": "P.O. Box 1",
"regionType": "1000008",
"country": "561",
"scopeID": "1",
"zipCode": "37312",
"security1": false,
"security2": false,
"security3": false,
"security4": false,
"security5": false
}
]
},
"resultName": "d5c65787396d4ac6a7de60af4b66c0a9"
},
{
"uri": "/v1/actors",
"httpMethod": "POST",
"requestBody": {
"actorCategoryID": "1",
"actorTypeDetails": {
"actorTypeID": "10000",
"actorSubTypeID": "10000",
"attorneyStatus": "10098",
"barNumber": "1234A12B",
"effectiveDate": "2022-11-21",
"barAdmittedDate": "2022-08-18"
},
"firstName": "Russell",
"middleName": "Luke",
"lastName": "Arp",
"scopeID": "1",
"contacts": [
{
"contactType": "400018",
"contactValue": "+1 4235556457",
"scopeID": "1",
"security1": false,
"security2": false,
"security3": false,
"security4": false,
"security5": false
},
{
"contactType": "23",
"contactValue": luke@lukesprojects.com,
"scopeID": "1",
"security1": false,
"security2": false,
"security3": false,
"security4": false,
"security5": false
},
{
"contactType": "24",
"contactValue": "+1 8564445541",
"scopeID": "1",
"security1": false,
"security2": false,
"security3": false,
"security4": false,
"security5": false
}
],
"addresses": [
{
"addressType": "21",
"city": "Athens",
"line1": "124 My St.",
"line2": "P.O. Box 1",
"regionType": "1000008",
"country": "561",
"scopeID": "1",
"zipCode": "37312",
"security1": false,
"security2": false,
"security3": false,
"security4": false,
"security5": false
}
]
},
"resultName": "9dfc64181f624d5eaf257c05e29c79dc"
}
]
}
--712975333129587337408149--
"@
$boundary = "nVenJ7H4puv"
    $body = ""
    for(var key in formHash){
        body += "--" + boundary
             + "\r\nContent-Disposition: form-data; name=" + formHash[key].name
             + "\r\nContent-type: " + formHash[key].type
             + "\r\n\r\n" + formHash[key].value + "\r\n"
    }
    body += "--" + boundary + "--\r\n"
$headers = @{
    Authorization="Basic "
}

Invoke-RestMethod -Method Post -Body $body -ContentType "multipart/form-data; boundry=712975333129587337408149" -Headers $headers