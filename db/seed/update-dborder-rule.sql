UPDATE rules
SET condition_json = '{"WindowMinutes":15,"MinOrders":1,"Channels":["SALESFORCE","MULTIVENDE"]}'
WHERE AppliesTo = 'DbOrderChecker';

SELECT AppliesTo, condition_json FROM rules WHERE AppliesTo = 'DbOrderChecker';
