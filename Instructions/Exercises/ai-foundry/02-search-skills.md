---
lab:
    title: 'Create a Custom Skill for Azure AI Search'
    module: 'Module 12 - Creating a Knowledge Mining Solution'
---

# Create a Custom Skill for Azure AI Search

Azure AI Search uses an enrichment pipeline of AI skills to extract AI-generated fields from documents and include them in a search index. There's a comprehensive set of built-in skills that you can use, but if you have a specific requirement that isn't met by these skills, you can create a custom skill.

In this exercise, you'll create a custom skill that tabulates the frequency of individual words in a document to generate a list of the top five most used words, and add it to a search solution for Margie's Travel - a fictitious travel agency.

## Create Azure resources

> **Note**: If you have previously completed the **[Create an Azure AI Search solution](01-azure-search.md)** exercise, and still have these Azure resources in your subscription, you can skip this section and start at the **Create a search solution** section. Otherwise, follow the steps below to provision the required Azure resources.

1. In a web browser, open the Azure portal at `https://portal.azure.com`, and sign in using the Microsoft account associated with your Azure subscription.
1. In the top search bar, search for *Azure AI services*, select **Azure AI services multi-service account**, and create an Azure AI services multi-service account resource with the following settings:
    - **Subscription**: *Your Azure subscription*
    - **Resource group**: *Choose or create a resource group (if you are using a restricted subscription, you may not have permission to create a new resource group - use the one provided)*
    - **Region**: *Choose from available regions geographically close to you*
    - **Name**: *Enter a unique name*
    - **Pricing tier**: Standard S0
1. Once deployed, go to the resource and on the **Overview** page, note the **Subscription ID** and **Location**. You will need these values, along with the name of the resource group in subsequent steps. 

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
   cd mslearn-knowledge-mining/Labfiles/02-search-skill
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
    - Search service endpoint
    - Search service admin key
    - Search service query key

1. In the Azure portal, refresh the resource group and verify that it contains the Azure Storage account, Azure AI Services resource, and Azure AI Search resource.

## Create a search solution

Now that you have the necessary Azure resources, you can create a search solution that consists of the following components:

- A **data source** that references the documents in your Azure storage container.
- A **skillset** that defines an enrichment pipeline of skills to extract AI-generated fields from the documents.
- An **index** that defines a searchable set of document records.
- An **indexer** that extracts the documents from the data source, applies the skillset, and populates the index.

In this exercise, you'll use the Azure AI Search REST interface to create these components by submitting JSON requests.

1. Run the following commands to open the **create-search** folder and edit **data_source.json**:

    ```powershell
    cd ./create-search
    code ./data_source.json
    ```

    This file contains a JSON definition for a data source named **margies-custom-data**.
   
1. Replace the **YOUR_CONNECTION_STRING** placeholder with the connection string for your Azure storage account, which should resemble the following:

    ```
    DefaultEndpointsProtocol=https;AccountName=ai102str123;AccountKey=12345abcdefg...==;EndpointSuffix=core.windows.net
    ```

    *You can find the connection string on the **Access keys** page for your storage account in the Azure portal.*

1. Save and close the updated JSON file.
1. In the command line pane, run `code skillset.json` to open **skillset.json**. This file contains a JSON definition for a skillset named **margies-custom-skillset**.
1. At the top of the skillset definition, in the **cognitiveServices** element, replace the **YOUR_AI_SERVICES_KEY** placeholder with either of the keys for your Azure AI Services resources.

    *You can find the keys on the **Keys and Endpoint** page for your Azure AI Services resource in the Azure portal.*

1. Save and close the updated JSON file.
1. In the command line pane, run `code index.json` to open **index.json**. This file contains a JSON definition for an index named **margies-custom-index**.
1. Review the JSON for the index, then close the file without making any changes.
1. In the command line pane, run `code indexer.json` to open **indexer.json**. This file contains a JSON definition for an indexer named **margies-custom-indexer**.
1. Review the JSON for the indexer, then close the file without making any changes.
1. In the command line pane, run `code create-search.sh` to open **create-search.sh**. This batch script uses the cURL utility to submit the JSON definitions to the REST interface for your Azure AI Search resource.
1. Replace the **YOUR_SEARCH_URL** and **YOUR_ADMIN_KEY** variable placeholders with the **Url** and one of the **admin keys** for your Azure AI Search resource.

    *You can find these values on the **Overview** and **Keys** pages for your Azure AI Search resource in the Azure portal.*

1. Save the updated batch file and close the code editor.
1. In the command line pane, enter the following commands to run the batch script:

    ```powershell
    chmod +x ./create-search.sh
    ./create-search.sh
    ```

1. When the script completes, in the Azure portal, on the page for your Azure AI Search resource, select the **Indexers** page and wait for the indexing process to complete.

    *You can select **Refresh** to track the progress of the indexing operation. It may take a minute or so to complete.*

## Search the index

Now that you have an index, you can search it.

1. At the top of the blade for your Azure AI Search resource, select **Search explorer**.
1. In Search explorer, in the **Query string** box, enter the following query string, and then select **Search**.

    ```
    search=London&$select=url,sentiment,keyphrases&$filter=metadata_author eq 'Reviewer' and sentiment eq 'positive'
    ```

    This query retrieves the **url**, **sentiment**, and **keyphrases** for all documents that mention *London* authored by *Reviewer* that have a positive **sentiment** label (in other words, positive reviews that mention London)

## Create an Azure Function for a custom skill

The search solution includes a number of built-in AI skills that enrich the index with information from the documents, such as the sentiment scores and lists of key phrases seen in the previous task.

You can enhance the index further by creating custom skills. For example, it might be useful to identify the words that are used most frequently in each document, but no built-in skill offers this functionality.

To implement the word count functionality as a custom skill, you'll create an Azure Function in your preferred language.

> **Note**: In this exercise, you'll create a simple Node.JS function using the code editing capabilities in the Azure portal. In a production solution, you would typically use a development environment such as Visual Studio Code to create a function app in your preferred language (for example C#, Python, Node.JS, or Java) and publish it to Azure as part of a DevOps process.

1. In the Azure Portal, on the **Home** page, create a new **Function App** resource with the following settings:
    - **Hosting Plan**: Consumption
    - **Subscription**: *Your subscription*
    - **Resource Group**: *The same resource group as your Azure AI Search resource*
    - **Function App name**: *A unique name*
    - **Runtime stack**: Node.js
    - **Version**: 18 LTS
    - **Region**: *The same region as your Azure AI Search resource*
    - **Operating system**: Windows

1. Wait for deployment to complete, and then go to the deployed Function App resource.
1. On the **Overview** page select **Create function** at the bottom of the page to create a new function with the following settings:
    - **Select a template**
        - **Template**: HTTP Trigger    
    - **Template details**:
        - **Function name**: wordcount
        - **Authorization level**: Function
1. Wait for the *wordcount* function to be created. Then in its page, select the **Code + Test** tab.
1. Replace the default function code with the following code:

    ```javascript
    module.exports = async function (context, req) {
        context.log('JavaScript HTTP trigger function processed a request.');
    
        if (req.body && req.body.values) {
    
            vals = req.body.values;
    
            // Array of stop words to be ignored
            var stopwords = ['', 'i', 'me', 'my', 'myself', 'we', 'our', 'ours', 'ourselves', 'you', 
            "youre", "youve", "youll", "youd", 'your', 'yours', 'yourself', 
            'yourselves', 'he', 'him', 'his', 'himself', 'she', "shes", 'her', 
            'hers', 'herself', 'it', "its", 'itself', 'they', 'them', 
            'their', 'theirs', 'themselves', 'what', 'which', 'who', 'whom', 
            'this', 'that', "thatll", 'these', 'those', 'am', 'is', 'are', 'was',
            'were', 'be', 'been', 'being', 'have', 'has', 'had', 'having', 'do', 
            'does', 'did', 'doing', 'a', 'an', 'the', 'and', 'but', 'if', 'or', 
            'because', 'as', 'until', 'while', 'of', 'at', 'by', 'for', 'with', 
            'about', 'against', 'between', 'into', 'through', 'during', 'before', 
            'after', 'above', 'below', 'to', 'from', 'up', 'down', 'in', 'out', 
            'on', 'off', 'over', 'under', 'again', 'further', 'then', 'once', 'here', 
            'there', 'when', 'where', 'why', 'how', 'all', 'any', 'both', 'each', 
            'few', 'more', 'most', 'other', 'some', 'such', 'no', 'nor', 'not', 
            'only', 'own', 'same', 'so', 'than', 'too', 'very', 'can', 'will',
            'just', "dont", 'should', "shouldve", 'now', "arent", "couldnt", 
            "didnt", "doesnt", "hadnt", "hasnt", "havent", "isnt", "mightnt", "mustnt",
            "neednt", "shant", "shouldnt", "wasnt", "werent", "wont", "wouldnt"];
    
            res = {"values":[]};
    
            for (rec in vals)
            {
                // Get the record ID and text for this input
                resVal = {recordId:vals[rec].recordId, data:{}};
                txt = vals[rec].data.text;
    
                // remove punctuation and numerals
                txt = txt.replace(/[^ A-Za-z_]/g,"").toLowerCase();
    
                // Get an array of words
                words = txt.split(" ")
    
                // count instances of non-stopwords
                wordCounts = {}
                for(var i = 0; i < words.length; ++i) {
                    word = words[i];
                    if (stopwords.includes(word) == false )
                    {
                        if (wordCounts[word])
                        {
                            wordCounts[word] ++;
                        }
                        else
                        {
                            wordCounts[word] = 1;
                        }
                    }
                }
    
                // Convert wordcounts to an array
                var topWords = [];
                for (var word in wordCounts) {
                    topWords.push([word, wordCounts[word]]);
                }
    
                // Sort in descending order of count
                topWords.sort(function(a, b) {
                    return b[1] - a[1];
                });
    
                // Get the first ten words from the first array dimension
                resVal.data.text = topWords.slice(0,9)
                  .map(function(value,index) { return value[0]; });
    
                res.values[rec] = resVal;
            };
    
            context.res = {
                body: JSON.stringify(res),
                headers: {
                'Content-Type': 'application/json'
            }
    
            };
        }
        else {
            context.res = {
                status: 400,
                body: {"errors":[{"message": "Invalid input"}]},
                headers: {
                'Content-Type': 'application/json'
            }
    
            };
        }
    };
    ```

1. Save the function and then open the **Test/Run** pane.
1. In the **Test/Run** pane, replace the existing **Body** with the following JSON, which reflects the schema expected by an Azure AI Search skill in which records containing data for one or more documents are submitted for processing:

    ```json
    {
        "values": [
            {
                "recordId": "a1",
                "data":
                {
                "text":  "Tiger, tiger burning bright in the darkness of the night.",
                "language": "en"
                }
            },
            {
                "recordId": "a2",
                "data":
                {
                "text":  "The rain in spain stays mainly in the plains! That's where you'll find the rain!",
                "language": "en"
                }
            }
        ]
    }
    ```

1. Click **Run** and view the HTTP response content that is returned by your function. This reflects the schema expected by Azure AI Search when consuming a skill, in which a response for each document is returned. In this case, the response consists of up to 10 terms in each document in descending order of how frequently they appear:

    ```json
    {
        "values": [
        {
            "recordId": "a1",
            "data": {
                "text": [
                "tiger",
                "burning",
                "bright",
                "darkness",
                "night"
                ]
            }
        },
        {
            "recordId": "a2",
            "data": {
                "text": [
                    "rain",
                    "spain",
                    "stays",
                    "mainly",
                    "plains",
                    "thats",
                    "youll",
                    "find"
                ]
            }
        }
        ]
    }
    ```

1. Close the **Test/Run** pane and in the **wordcount** function blade, click **Get function URL**. Then copy the URL for the default key to the clipboard. You'll need this in the next procedure.

## Add the custom skill to the search solution

Now you need to include your function as a custom skill in the search solution skillset, and map the results it produces to a field in the index. 

1. In the command line pane, run `cd ../update-search` to open the **update-search** folder, then run `code update-skillset.json` to edit the **update-skillset.json** file. This contains the JSON definition of a skillset.
1. Review the skillset definition. It includes the same skills as before, as well as a new **WebApiSkill** skill named **get-top-words**.
1. Edit the **get-top-words** skill definition to set the **uri** value to the URL for your Azure function (which you copied to the clipboard in the previous procedure), replacing **YOUR-FUNCTION-APP-URL**.
1. At the top of the skillset definition, in the **cognitiveServices** element, replace the **YOUR_AI_SERVICES_KEY** placeholder with either of the keys for your Azure AI Services resources.

    *You can find the keys on the **Keys and Endpoint** page for your Azure AI Services resource in the Azure portal.*

1. Save and close the updated JSON file.
1. In the command line pane, run `code update-index.json` to open **update-index.json**. This file contains the JSON definition for the **margies-custom-index** index, with an additional field named **top_words** at the bottom of the index definition.
1. Review the JSON for the index, then close the file without making any changes.
1. In the command line pane, run `code update-indexer.json` to open **update-indexer.json**. This file contains a JSON definition for the **margies-custom-indexer**, with an additional mapping for the **top_words** field.
1. Review the JSON for the indexer, then close the file without making any changes.
1. In the command line pane, run `code update-search.sh` to open **update-search.sh**. This batch script uses the cURL utility to submit the updated JSON definitions to the REST interface for your Azure AI Search resource.
1. Replace the **YOUR_SEARCH_URL** and **YOUR_ADMIN_KEY** variable placeholders with the **Url** and one of the **admin keys** for your Azure AI Search resource.

    *You can find these values on the **Overview** and **Keys** pages for your Azure AI Search resource in the Azure portal.*

1. Save the updated batch file and close the code editor.
1. In the command line pane, enter the following commands to run the batch script:

    ```powershell
    chmod +x ./update-search.sh
    ./update-search.sh
    ```
15. When the script completes, in the Azure portal, on the page for your Azure AI Search resource, select the **Indexers** page and wait for the indexing process to complete.

    *You can select **Refresh** to track the progress of the indexing operation. It may take a minute or so to complete.*

## Search the index

Now that you have an index, you can search it.

1. At the top of the blade for your Azure AI Search resource, select **Search explorer**.
2. In Search explorer, change the view to **JSON view**, and then submit the following search query:

    ```json
    {
      "search": "Las Vegas",
      "select": "url,top_words"
    }
    ```

    This query retrieves the **url** and **top_words** fields for all documents that mention *Las Vegas*.

## Clean-up

Now that you've completed the exercise, delete all the resources you no longer need. Delete the Azure resources:

1. In the Azure portal, select **Resource groups**.
1. Select the resource group you don't need, then select **Delete resource group**.

## More information

To learn more about creating custom skills for Azure AI Search, see the [Azure AI Search documentation](https://docs.microsoft.com/azure/search/cognitive-search-custom-skill-interface).
