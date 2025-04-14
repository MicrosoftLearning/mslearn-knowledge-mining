#!/bin/bash

# Set values for your storage account
subscription_id="YOUR_SUBSCRIPTION_ID"
azure_storage_account="YOUR_AZURE_STORAGE_ACCOUNT_NAME"
azure_storage_key="YOUR_AZURE_STORAGE_KEY"

echo "Creating container..."
az storage container create \
  --account-name "$azure_storage_account" \
  --subscription "$subscription_id" \
  --name margies \
  --auth-mode key \
  --account-key "$azure_storage_key" \
  --output none

echo "Uploading files..."
az storage blob upload-batch \
  -d margies \
  -s data \
  --account-name "$azure_storage_account" \
  --auth-mode key \
  --account-key "$azure_storage_key" \
  --output none
