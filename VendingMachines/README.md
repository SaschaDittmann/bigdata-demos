# Installing the Big Data Demo "Vending Machines"

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FSaschaDittmann%2Fbigdata-demos%2Fmaster%2FVendingMachines%2FVendingMachines-ARM%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2FSaschaDittmann%2Fbigdata-demos%2Fmaster%2FVendingMachines%2FVendingMachines-ARM%2Fazuredeploy.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>

Overview
--------
Trey Research Inc. looks at the old way of doing things in retail and introduces innovative experiences that delight customers and drive sales. Their latest initiative focuses on intelligent vending machines that support commerce, engagement analytics, and intelligent promotions.

In this Hands-on-lab, attendees will construct an end-to-end solution for an IoT scenario that includes device management; telemetry ingest; hot and cold path processing; and reporting.

How to Run the scripts
----------------------

You can use the Deploy to Azure button or use the below methor with powershell

Creating a new deployment with powershell:

Remember to set your Username, Password and Unique Storage Account name in azuredeploy-parameters.json

Create a resource group:

    PS C:\> New-AzureResourceGroup -Name "mrsonspark" -Location 'NorthEurope'

Start deployment

    PS C:\> New-AzureResourceGroupDeployment -Name vendingmachines-deployment -ResourceGroupName "mrsonspark" -TemplateFile C:\gitsrc\bigdata-demos\master\VendingMachines\VendingMachines-ARM\azuredeploy.json -TemplateParameterFile C:\gitsrc\bigdata-demos\master\VendingMachines\VendingMachines-ARM\azuredeploy.parameters.json -Verbose
 