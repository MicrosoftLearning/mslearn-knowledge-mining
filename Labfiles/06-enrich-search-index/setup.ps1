# PowerShell script to set up Azure ML resources

# Generate a random string
$guid = [guid]::NewGuid().ToString()
$suffix = $guid -replace '-', ''
$suffix = $suffix.Substring(0, 18)

# Set the necessary variables
$RESOURCE_GROUP = "rg-knowledge-m$suffix"
$RESOURCE_PROVIDER = "Microsoft.MachineLearningServices"
$REGIONS = @("eastus", "westus", "centralus", "northeurope", "westeurope")
$RANDOM_REGION = $REGIONS | Get-Random
$WORKSPACE_NAME = "mlw-knowledge-m$suffix"
$COMPUTE_CLUSTER = "aml-cluster"

# Register the Azure Machine Learning resource provider in the subscription
Write-Host "Register the Machine Learning resource provider:"
az provider register --namespace $RESOURCE_PROVIDER

# Create the resource group and workspace and set to default
Write-Host "Create a resource group and set as default:"
az group create --name $RESOURCE_GROUP --location $RANDOM_REGION
az configure --defaults group=$RESOURCE_GROUP

Write-Host "Create an Azure Machine Learning workspace:"
az ml workspace create --name $WORKSPACE_NAME
az configure --defaults workspace=$WORKSPACE_NAME

# Create compute cluster
Write-Host "Creating a compute cluster with name: $COMPUTE_CLUSTER"
az ml compute create --name $COMPUTE_CLUSTER --size STANDARD_DS11_V2 --max-instances 2 --type AmlCompute

# Create data asset
Write-Host "Creating a data asset with name: car-folder"
az ml data create --name car-folder --path ./data

# Create components
Write-Host "Creating components"
az ml component create --file ./train-linear-regression.yml
