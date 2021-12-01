# About
This is a tool to help automate requests to the **SigParser API** and generate JSON files. This uses delta style requests and some local caching to efficiently get the results.

# How To Use
1. Find the latest release from the precompiled **Releases** in GitHub
1. Download the zip file that corresponds to your **OS**.  
2. **Extract** the zip to a location where you would like the application to be.
3. (Optional) Add a new environment variable called **SigParserApiKey**, with the value of your SigParser API key.

## Show Help
```
sigparser-api --help
```

## Fetch Contacts
This shows how to get a file with all the contacts from SigParser into a JSON file. This can generate a JSON array file or a JSON lines file which might be easier to process.  
```
sigparser-api fetch-contacts --output contacts.json --apikey XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
```

There is a local cache of contacts. If you need to reset the local cache then be sure to delete the state file as well as all the JSON files for all the contacts. 
