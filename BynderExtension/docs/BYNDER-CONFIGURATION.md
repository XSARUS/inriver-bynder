
# Bynder configuration

These instructions help you configure your Bynder account for intergration with inRiver.

## Prerequisites

You will need the following setup to configure Bynder.

- Bynder account.
- Amazon Simple Notification Service (Bynder needs set this this for you)

## Subscribe To Amazon SNS
Bynder uses Amazon SNS for messaging. Ask bynder to set this up for you.
You need to give them an end-point to receive asset updates on that end-point. 

The URL should look something like this: https://inbound.productmarketingcloud.com/api/inbounddata/[CUSTOMERNAME]/[ENVIRONMENT]/[EXSTENSIONID]

- [CUSTOMERNAME] = the name of your customer
- [ENVIRONMENT] = one of your configured environments production/test
- [EXTENSIONID] = we wil use 'BynderAssetNotify' later on in the inRiver documentation. 
  So that one we will use. But you're free to use your own extension Id.

## Create a consumer
To create a consumer you have to login in to your Bynder account.

- Go to https://[YOURBYNDERACCOUNT].getbynder.com/pysettings
- Add new consumer
- Store the 'consumerkey' and 'consumersecret' someware safe. We're going to use this later.

## Create a token
 In the same Settings menu as the previous step,
- Add a new Token
- Select the previous created consumer
- Store the 'username', 'token', 'secret' someware safe. We need this later.

## Add metaproperties
If you want to update a Bynder asset with information from inRiver,
you can add metaproperties to store values on a asset. 

To create a metaproperty:

- Go to your Bynder account
- Settings -> Account -> Metaproperties management
- Press Add
- For our implementation we use:
  - Name = ResourceDescription
  - Label = ResourceDescription
  - Type = Text
- Press Save

It's good practice to use metaproperty names similar to fieldTypeId's in your inRiver model.
For this demonstration 'ResourceDescription' will be added in our inRiver model later.

In the URL you wil find the Id of the metaproperty. Write it down for later use!