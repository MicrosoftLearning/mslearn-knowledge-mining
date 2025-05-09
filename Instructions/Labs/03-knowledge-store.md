---
lab:
    title: 'Create a Knowledge Store with Azure AI Search'
    module: 'Module 12 - Creating a Knowledge Mining Solution'
---

# Create a Knowledge Store with Azure AI Search

Azure AI Search uses an enrichment pipeline of AI skills to extract AI-generated fields from documents and include them in a search index. While the index might be considered the primary output from an indexing process, the enriched data it contains might also be useful in other ways. For example:

- Since the index is essentially a collection of JSON objects, each representing an indexed record, it might be useful to export the objects as JSON files for integration into a data orchestration process using tools such as Azure Data Factory.
- You may want to normalize the index records into a relational schema of tables for analysis and reporting with tools such as Microsoft Power BI.
- Having extracted embedded images from documents during the indexing process, you might want to save those images as files.

In this exercise, you'll implement a knowledge store for *Margie's Travel*, a fictitious travel agency that uses information in brochures and hotel reviews to help customers plan trips.

## Create Azure resources

> **Note**: If you have previously completed the **[Create an Azure AI Search solution](01-azure-search.md)** exercise, and still have these Azure resources in your subscription, you can skip this section and start at the **Create a search solution** section. Otherwise, follow the steps below to provision the required Azure resources.

1. In a web browser, open the Azure portal at `https://portal.azure.com`, and sign in using the Microsoft account associated with your Azure subscription.
1. View the **Resource groups** in your subscription.
1. If you are using a restricted subscription in which a resource group has been provided for you, select the resource group to view its properties. Otherwise, create a new resource group with a name of your choice, and go to it when it has been created.
1. On the **Overview** page for your resource group, note the **Subscription ID** and **Location**. You will need these values, along with the name of the resource group in subsequent steps.

## Prepare to develop an app in Cloud Shell
You'll develop your search app using Azure cloud shell. The code files for your app have been provided in a GitHub repo.

> **Tip**: If you have already cloned the **mslearn-knowledge-mining** repo, you can skip this task. Otherwise, follow these steps to clone it to your development environment.

1. Use the **[\>_]** button to the right of the search bar at the top of the page to create a new Cloud Shell in the Azure portal, selecting a ***PowerShell*** environment with no storage in your subscription.

    The cloud shell provides a command-line interface in a pane at the bottom of the Azure portal. You can resize or maximize this pane to make it easier to work in.

    > **Note**: If you have previously created a cloud shell that uses a *Bash* environment, switch it to ***PowerShell***.

1. In the cloud shell toolbar, in the **Settings** menu, select **Go to Classic version** (this is required to use the code editor).

    **<font color="red">Ensure you've switched to the classic version of the cloud shell before continuing.</font>**

1. In the cloud shell pane, enter the following commands to clone the GitHub repo containing the code files for this exercise (type the command, or copy it to the clipboard and then right-click in the command line and paste as plain text):

    ```
    rm -r mslearn-knowledge-mining -f
    git clone https://github.com/microsoftlearning/mslearn-knowledge-mining mslearn-knowledge-mining
    ```

    > **Tip**: As you enter commands into the cloudshell, the output may take up a large amount of the screen buffer. You can clear the screen by entering the `cls` command to make it easier to focus on each task.

1. After the repo has been cloned, navigate to the folder containing the application code files:

    ```
   cd mslearn-knowledge-mining/Labfiles/03-knowledge-store
    ```

1. Run the following command to list Azure locations.

    ```powershell
    az account list-locations -o table
    ```

1. In the output, find the **Name** value that corresponds with the location of your resource group (for example, for *East US* the corresponding name is *eastus*).
1. Enter the following command to edit the batch script which contains the Azure command line interface (CLI) commands required to create the Azure resources you need:

    ```powershell
    code ./setup.sh
    ```

   The file is opened in a code editor.

1. In the code file, replace the **subscription_id**, **resource_group**, and **location** variable declarations with the appropriate values for your subscription ID, resource group name, and location name.
1. After you've replaced the placeholders, within the code editor, use the **CTRL+S** command or **Right-click > Save** to save your changes and then use the **CTRL+Q** command or **Right-click > Quit** to close the code editor while keeping the cloud shell command line open.
1. In the command line pane, enter the following commands to run the script:

    ```powershell
    chmod +x ./setup.sh
    ./setup.sh
    ```

    > **Note**: If the script fails, ensure you saved it with the correct variable names and try again.

1. When the script completes, review the output it displays and note the following information about your Azure resources (you will need these values later):
    - Storage account name
    - Storage connection string
    - Azure AI Services account
    - Azure AI Services key
    - Search service endpoint
    - Search service admin key
    - Search service query key

1. In the Azure portal, refresh the resource group and verify that it contains the Azure Storage account, Azure AI Services resource, and Azure AI Search resource.

## Create a search solution

Now that you have the necessary Azure resources, you can create a search solution that consists of the following components:

- A **data source** that references the documents in your Azure storage container.
- A **skillset** that defines an enrichment pipeline of skills to extract AI-generated fields from the documents. The skillset also defines the *projections* that will be generated in your *knowledge store*.
- An **index** that defines a searchable set of document records.
- An **indexer** that extracts the documents from the data source, applies the skillset, and populates the index. The process of indexing also persists the projections defined in the skillset in the knowledge store.

In this exercise, you'll use the Azure AI Search REST interface to create these components by submitting JSON requests.

### Prepare JSON for REST operations

You'll use the REST interface to submit JSON definitions for your Azure AI Search components.

1. Run the following commands to open the **create-search** folder and edit **data_source.json**:

    ```powershell
    cd ./create-search
    code ./data_source.json
    ```

    This file contains a JSON definition for a data source named **margies-knowledge-data**.

1. Replace the **YOUR_CONNECTION_STRING** placeholder with the connection string for your Azure storage account, which should resemble the following:

    ```
    DefaultEndpointsProtocol=https;AccountName=ai102str123;AccountKey=12345abcdefg...==;EndpointSuffix=core.windows.net
    ```

    *You can find the connection string on the **Access keys** page for your storage account in the Azure portal.*

1. Save and close the updated JSON file.
1. In the command line pane, run `code skillset.json` to open **skillset.json**. This file contains a JSON definition for a skillset named **margies-knowledge-skillset**.
1. At the top of the skillset definition, in the **cognitiveServices** element, replace the **YOUR_COGNITIVE_SERVICES_KEY** placeholder with either of the keys for your Azure AI Services resources.

    *You can find the keys on the **Keys and Endpoint** page for your Azure AI Services resource in the Azure portal.*

1. At the end of the collection of skills in your skillset, find the **Microsoft.Skills.Util.ShaperSkill** skill named **define-projection**. This skill defines a JSON structure for the enriched data that will be used for the projections that the pipeline will persist on the knowledge store for each document processed by the indexer.
1. At the bottom of the skillset file, observe that the skillset also includes a **knowledgeStore** definition, which includes a connection string for the Azure Storage account where the knowledge store is to be created, and a collection of **projections**. This skillset includes three *projection groups*:
    - A group containing an *object* projection based on the **knowledge_projection** output of the shaper skill in the skillset.
    - A group containing a *file* projection based on the **normalized_images** collection of image data extracted from the documents.
    - A group containing the following *table* projections:
        - **KeyPhrases**: Contains an automatically generated key column and a **keyPhrase** column mapped to the **knowledge_projection/key_phrases/** collection output of the shaper skill.
        - **Locations**: Contains an automatically generated key column and a **location** column mapped to the **knowledge_projection/key_phrases/** collection output of the shaper skill.
        - **ImageTags**: Contains an automatically generated key column and a **tag** column mapped to the **knowledge_projection/image_tags/** collection output of the shaper skill.
        - **Docs**: Contains an automatically generated key column and all of the **knowledge_projection** output values from the shaper skill that are not already assigned to a table.
1. Replace the **YOUR_CONNECTION_STRING** placeholder for the **storageConnectionString** value with the connection string for your storage account.
1. Save and close the updated JSON file.
1. In the command line pane, run `code index.json` to open **index.json**. This file contains a JSON definition for an index named **margies-knowledge-index**.
1. Review the JSON for the index, then close the file without making any changes.
1. In the command line pane, run `code indexer.json` to open **indexer.json**. This file contains a JSON definition for an indexer named **margies-knowledge-indexer**.
1. Review the JSON for the indexer, then close the file without making any changes.

### Submit REST requests

Now that you've prepared the JSON objects that define your search solution components, you can submit the JSON documents to the REST interface to create them.

1. In the command line pane, run `code create-search.sh` to open **create-search.sh**. This batch script uses the cURL utility to submit the JSON definitions to the REST interface for your Azure AI Search resource.
2. Replace the **YOUR_SEARCH_URL** and **YOUR_ADMIN_KEY** variable placeholders with the **Url** and one of the **admin keys** for your Azure AI Search resource.

    *You can find these values on the **Overview** and **Keys** pages for your Azure AI Search resource in the Azure portal.*

3. Save the updated batch file and close the code editor.
1. In the command line pane, enter the following commands to run the batch script:

    ```powershell
    chmod +x ./create-search.sh
    ./create-search.sh
    ```

6. When the script completes, in the Azure portal, on the page for your Azure AI Search resource, select the **Indexers** page and wait for the indexing process to complete.

    *You can select **Refresh** to track the progress of the indexing operation. It may take a minute or so to complete.*

    > **Tip**: If the script fails, check the placeholders you added in the **data_source.json** and **skillset.json** files as well as the **create-search.cmd** file. After correcting any mistakes, you may need to use the Azure portal user interface to delete any components that were created in your search resource before re-running the script.

## View the knowledge store

After you have run an indexer that uses a skillset to create a knowledge store, the enriched data extracted by the indexing process is persisted in the knowledge store projections.

### View object projections

The *object* projections defined in the Margie's Travel skillset consist of a JSON file for each indexed document. These files are stored in a blob container in the Azure Storage account specified in the skillset definition.

1. In the Azure portal, view the Azure Storage account you created previously.
1. Select the **Storage browser** tab (in the pane on the left) to view the storage account in the storage explorer interface in the Azure portal.
1. Expand **Blob containers** to view the containers in the storage account. In addition to the **margies** container where the source data is stored, there should be two new containers: **margies-images** and **margies-knowledge**. These were created by the indexing process.
1. Select the **margies-knowledge** container. It should contain a folder for each indexed document.
1. Open any of the folders, and then download and open the **knowledge-projection.json** file it contains. Each JSON file contains a representation of an indexed document, including the enriched data extracted by the skillset as shown here.

    ```json
    {
        "file_id":"abcd1234....",
        "file_name":"Margies Travel Company Info.pdf",
        "url":"https://store....blob.core.windows.net/margies/...pdf",
        "language":"en",
        "sentiment":0.83164644241333008,
        "key_phrases":[
            "Margie’s Travel",
            "Margie's Travel",
            "best travel experts",
            "world-leading travel agency",
            "international reach"
            ],
        "locations":[
            "Dubai",
            "Las Vegas",
            "London",
            "New York",
            "San Francisco"
            ],
        "image_tags":[
            "outdoor",
            "tree",
            "plant",
            "palm"
            ]
    }
    ```

The ability to create *object* projections like this enables you to generate enriched data objects that can be incorporated into an enterprise data analysis solution - for example by ingesting the JSON files into an Azure Data Factory pipeline for further processing or loading into a data warehouse.

### View file projections

The *file* projections defined in the skillset create JPEG files for each image that was extracted from the documents during the indexing process.

1. In the *Storage browser* interface in the Azure portal, select the **margies-images** blob container. This container contains a folder for each document that contained images.
2. Open any of the folders and view its contents - each folder contains at least one \*.jpg file.
3. Open any of the image files to verify that they contain images extracted from the documents.

The ability to generate *file* projections like this makes indexing an efficient way to extract embedded images from a large volume of documents.

### View table projections

The *table* projections defined in the skillset form a relational schema of enriched data.

1. In the *Storage browser* interface in the Azure portal, expand **Tables**.
2. Select the **docs** table to view its columns. The columns include some standard Azure Storage table columns - to hide these, modify the **Column Options** to select only the following columns:
    - **document_id** (the key column automatically generated by the indexing process)
    - **file_id** (the encoded file URL)
    - **file_name** (the file name extracted from the document metadata)
    - **language** (the language in which the document is written)
    - **sentiment** the sentiment score calculated for the document.
    - **url** the URL for the document blob in Azure storage.
3. View the other tables that were created by the indexing process:
    - **ImageTags** (contains a row for each individual image tag with the **document_id** for the document in which the tag appears).
    - **KeyPhrases** (contains a row for each individual key phrase with the **document_id** for the document in which the phrase appears).
    - **Locations** (contains a row for each individual location with the **document_id** for the document in which the location appears).

The ability to create *table* projections enables you to build analytical and reporting solutions that query the relational schema; for example, using Microsoft Power BI. The automatically generated key columns can be used to join the tables in queries - for example to return all of the locations mentioned in a specific document.

## Clean-up

Now that you've completed the exercise, delete all the resources you no longer need. Delete the Azure resources:

1. In the Azure portal, select **Resource groups**.
1. Select the resource group you don't need, then select **Delete resource group**.

## More information

To learn more about creating knowledge stores with Azure AI Search, see the [Azure AI Search documentation](https://docs.microsoft.com/azure/search/knowledge-store-concept-intro).
