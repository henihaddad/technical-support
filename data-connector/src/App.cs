using Curiosity.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TechnicalSupport;
using static TechnicalSupport.Schema;
using UID;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Net.Http;

string token = Environment.GetEnvironmentVariable("CURIOSITY_API_TOKEN");
string endpointToken = Environment.GetEnvironmentVariable("CURIOSITY_ENDPOINTS_TOKEN");

if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(endpointToken))
{
    PrintHelp();
    return;
}

var loggerFactory = LoggerFactory.Create(l => l.AddConsole());
var logger = loggerFactory.CreateLogger("Data Connector");

using (var graph = Graph.Connect("http://localhost:8080/", token, "Curiosity Connector").WithLoggingFactory(loggerFactory))
{
    loggerFactory.AddProvider(graph.GetServerLoggingProvider());

    try
    {
        logger.LogInformation("Creating schemas");
        await CreateSchemasAsync(graph);

        logger.LogInformation("Ingesting data");
        await UploadDataAsync(graph);
        logger.LogInformation("Done with local data");

        logger.LogInformation("Ingesting STAPI data");
        await UploadStapiDataAsync(graph);
        logger.LogInformation("Done with STAPI data");

        var response = await graph.QueryAsync(q => q.StartAt(nameof(Nodes.Device)).EmitCount("C"));
        var count = response.GetEmittedCount("C");

        var response2 = await graph.QueryAsync(q => q.StartAt(nameof(Nodes.Device)).Take(10).Emit("N", [nameof(Nodes.Device.Name)]));
        var nodes = response2.GetEmitted("N").ToDictionary(n => n.UID, n => n.GetField<string>(nameof(Nodes.Device.Name)));

        logger.LogInformation("Finished data connector");
    }
    catch(Exception E)
    {
        logger.LogError(E, "Error running data connector");
        throw;
    }
}

await TestEndpointsAsync(endpointToken);


void PrintHelp()
{
    Console.WriteLine("Missing tokens, you can set it using the CURIOSITY_API_TOKEN and CURIOSITY_ENDPOINTS_TOKEN environment variables");
}

async Task CreateSchemasAsync(Graph graph)
{
    await graph.CreateNodeSchemaAsync<Nodes.Device>();
    await graph.CreateNodeSchemaAsync<Nodes.Part>();
    await graph.CreateNodeSchemaAsync<Nodes.Manufacturer>();
    await graph.CreateNodeSchemaAsync<Nodes.SupportCase>();
    await graph.CreateNodeSchemaAsync<Nodes.SupportCaseMessage>();
    await graph.CreateNodeSchemaAsync<Nodes.Status>();
    await graph.CreateNodeSchemaAsync<Nodes.SupportChatContext>();
    await graph.CreateNodeSchemaAsync<Nodes.Character>();
    await graph.CreateNodeSchemaAsync<Nodes.Species>();
    await graph.CreateNodeSchemaAsync<Nodes.Organization>();
    await graph.CreateEdgeSchemaAsync(typeof(Edges));
}

async Task UploadDataAsync(Graph graph)
{
    var devices = JsonConvert.DeserializeObject<DeviceJson[]>(File.ReadAllText(Path.Combine("..", "data", "devices.json")));
    var parts   = JsonConvert.DeserializeObject<PartJson[]>(File.ReadAllText(Path.Combine("..", "data", "parts.json")));
    var cases   = JsonConvert.DeserializeObject<SupportCaseJson[]>(File.ReadAllText(Path.Combine("..", "data", "support-cases.json")));

    logger.LogInformation("Ingesting {0:n0} devices", devices.Length);
    foreach (var device in devices)
    {
        var devideNode = graph.TryAdd(new Nodes.Device() { Name = device.Name });
        graph.AddAlias(devideNode, Mosaik.Core.Language.Any, device.Name.Replace("-", " "), ignoreCase: false);
        graph.AddAlias(devideNode, Mosaik.Core.Language.Any, device.Name.Replace("-", "."), ignoreCase: false);
    }

    logger.LogInformation("Ingesting {0:n0} parts", parts.Length);
    foreach (var part in parts)
    {
        var partNode = graph.TryAdd(new Nodes.Part() { Name = part.Name });

        if (!string.IsNullOrWhiteSpace(part.Manufacturer))
        {
            var manufacturerNode = graph.TryAdd(new Nodes.Manufacturer() { Name = part.Manufacturer });
            graph.Link(partNode, manufacturerNode, Edges.HasManufacturer, Edges.ManufacturerOf);
        }

        foreach (var device in part.Devices)
        {
            graph.Link(partNode, Node.FromKey(nameof(Nodes.Device), device), Edges.PartOf, Edges.HasPart);
        }
    }

    var supportCaseId = 0;
    logger.LogInformation("Ingesting {0:n0} cases", cases.Length);
    foreach (var supportCase in cases.OrderBy(t => t.Time))
    {
        var supportCaseNode = graph.TryAdd(new Nodes.SupportCase() { Id = $"SC-{supportCaseId:0000}", Content = supportCase.Content, CaseSummary = supportCase.Summary, Time = supportCase.Time, Status = supportCase.Status });

        var statusNode = graph.TryAdd(new Nodes.Status { Value = supportCase.Status });
        graph.UnlinkExcept(supportCaseNode, statusNode, Edges.HasStatus, Edges.StatusOf);
        graph.Link(supportCaseNode, statusNode, Edges.HasStatus, Edges.StatusOf);

        graph.Link(supportCaseNode, Node.FromKey(nameof(Nodes.Device), supportCase.Device), Edges.ForDevice, Edges.HasSupportCase);

        var sb = new StringBuilder();
        bool isUser = false;
        int msgId = 0;
        var time = supportCase.Time;
        foreach (var line in supportCase.Content.Split(['\r','\n']))
        {
            if(line.StartsWith("User: "))
            {
                if(sb.Length > 0)
                {
                    var msgNode = graph.AddOrUpdate(new Nodes.SupportCaseMessage() { Id = $"SC-{supportCaseId:0000}-{msgId:000}", Author = isUser ? "User" : "Support", Message = sb.ToString(), Time = time });
                    graph.Link(supportCaseNode, msgNode, Edges.HasMessage, Edges.MessageOf);
                    time += TimeSpan.FromSeconds(Random.Shared.Next(60) * Random.Shared.Next(60));
                    msgId++;
                    sb.Length = 0;
                }
                isUser = true;
                sb.AppendLine(line.Substring("User: ".Length));
            }
            else if (line.StartsWith("Support: "))
            {
                if (sb.Length > 0)
                {
                    var msgNode = graph.AddOrUpdate(new Nodes.SupportCaseMessage() { Id = $"SC-{supportCaseId:0000}-{msgId:000}", Author = isUser ? "User" : "Support", Message = sb.ToString(), Time = time });
                    graph.Link(supportCaseNode, msgNode, Edges.HasMessage, Edges.MessageOf);
                    time += TimeSpan.FromSeconds(Random.Shared.Next(60) * Random.Shared.Next(60));
                    msgId++;
                    sb.Length = 0;
                }
                isUser = false;
                sb.AppendLine(line.Substring("Support: ".Length));
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        if (sb.Length > 0)
        {
            var msgNode = graph.AddOrUpdate(new Nodes.SupportCaseMessage() { Id = $"SC-{supportCaseId:0000}-{msgId:000}", Author = isUser ? "User" : "Support", Message = sb.ToString(), Time = time });
            graph.Link(supportCaseNode, msgNode, Edges.HasMessage, Edges.MessageOf);
            time += TimeSpan.FromSeconds(Random.Shared.Next(60) * Random.Shared.Next(60));
            msgId++;
            sb.Length = 0;
        }

        supportCaseId++;
    }

    await graph.CommitPendingAsync();
}


async Task UploadStapiDataAsync(Graph graph)
{
    var http = new HttpClient();
    http.BaseAddress = new Uri("https://stapi.co/api/v1/rest/");

    // Fetch all species
    var allSpecies = new List<StapiSpecies>();
    int page = 0;
    while (true)
    {
        var json = await http.GetStringAsync($"species/search?pageNumber={page}&pageSize=50");
        var result = JsonConvert.DeserializeObject<StapiSpeciesResponse>(json);
        allSpecies.AddRange(result.Species);
        if (result.Page.LastPage) break;
        page++;
    }

    logger.LogInformation("Ingesting {0:n0} species", allSpecies.Count);
    foreach (var species in allSpecies)
    {
        var traits = new List<string>();
        if (species.HumanoidSpecies == true)   traits.Add("Humanoid");
        if (species.ReptilianSpecies == true)  traits.Add("Reptilian");
        if (species.TelepathicSpecies == true) traits.Add("Telepathic");
        if (species.WarpCapableSpecies == true) traits.Add("Warp-capable");
        if (species.ExtinctSpecies == true)    traits.Add("Extinct");

        var desc = traits.Count > 0 ? string.Join(", ", traits) : "";
        var homeworld = species.Homeworld?.Name ?? "";

        var speciesNode = graph.TryAdd(new Nodes.Species()
        {
            Uid = species.Uid,
            Name = species.Name,
            Homeworld = homeworld,
            Description = desc
        });
        graph.AddAlias(speciesNode, Mosaik.Core.Language.Any, species.Name, ignoreCase: false);
    }

    // Fetch all organizations
    var allOrgs = new List<StapiOrganization>();
    page = 0;
    while (true)
    {
        var json = await http.GetStringAsync($"organization/search?pageNumber={page}&pageSize=50");
        var result = JsonConvert.DeserializeObject<StapiOrganizationResponse>(json);
        allOrgs.AddRange(result.Organizations);
        if (result.Page.LastPage) break;
        page++;
    }

    logger.LogInformation("Ingesting {0:n0} organizations", allOrgs.Count);
    foreach (var org in allOrgs)
    {
        var types = new List<string>();
        if (org.Government == true)              types.Add("Government");
        if (org.MilitaryOrganization == true)    types.Add("Military");
        if (org.MedicalOrganization == true)     types.Add("Medical");
        if (org.ResearchOrganization == true)    types.Add("Research");
        if (org.SportOrganization == true)       types.Add("Sport");

        var desc = types.Count > 0 ? string.Join(", ", types) : "";

        var orgNode = graph.TryAdd(new Nodes.Organization()
        {
            Uid = org.Uid,
            Name = org.Name,
            Description = desc
        });
        graph.AddAlias(orgNode, Mosaik.Core.Language.Any, org.Name, ignoreCase: false);
    }

    var allCharacters = new List<StapiCharacter>();
    int maxPages = 20;
    page = 0;
    while (page < maxPages)
    {
        var json = await http.GetStringAsync($"character/search?pageNumber={page}&pageSize=50");
        var result = JsonConvert.DeserializeObject<StapiCharacterResponse>(json);
        allCharacters.AddRange(result.Characters);
        if (result.Page.LastPage) break;
        page++;
    }

    logger.LogInformation("Ingesting {0:n0} characters with relationships", allCharacters.Count);
    int count = 0;
    foreach (var character in allCharacters)
    {
        var charNode = graph.TryAdd(new Nodes.Character()
        {
            Uid = character.Uid,
            Name = character.Name,
            Gender = character.Gender ?? "",
            YearOfBirth = character.YearOfBirth?.ToString() ?? "",
            YearOfDeath = character.YearOfDeath?.ToString() ?? ""
        });
        graph.AddAlias(charNode, Mosaik.Core.Language.Any, character.Name, ignoreCase: false);

        // Fetch character detail to get species and organization links
        try
        {
            var detailJson = await http.GetStringAsync($"character?uid={character.Uid}");
            var detail = JsonConvert.DeserializeObject<StapiCharacterFull>(detailJson);

            if (detail?.Character?.CharacterSpecies != null)
            {
                foreach (var sp in detail.Character.CharacterSpecies)
                {
                    var speciesNode = Node.FromKey(nameof(Nodes.Species), sp.Uid);
                    graph.Link(speciesNode, charNode, Edges.HasCharacter, Edges.CharacterOf);
                }
            }

            if (detail?.Character?.Organizations != null)
            {
                foreach (var org in detail.Character.Organizations)
                {
                    var orgNode = Node.FromKey(nameof(Nodes.Organization), org.Uid);
                    graph.Link(orgNode, charNode, Edges.HasMember, Edges.MemberOf);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to fetch detail for {0}: {1}", character.Name, ex.Message);
        }

        count++;
        if (count % 50 == 0)
        {
            logger.LogInformation("Processed {0}/{1} characters", count, allCharacters.Count);
        }
    }

    await graph.CommitPendingAsync();
}

async Task TestEndpointsAsync(string endpointToken)
{
    //Endpoints can be called using the EndpointsClient wrapper class.
    var endpointClient = new EndpointsClient("http://localhost:8080/", endpointToken);
    
    var responseHelloWorld = await endpointClient.CallAsync<string>("hello-world");
    Console.WriteLine($"Endpoint 'hello-world' answered with {responseHelloWorld}");

    var responsePooling = await endpointClient.CallAsync<string>("long-running-hello-world");
    Console.WriteLine($"Endpoint 'long-running-hello-world' answered with {responsePooling}");

    var responseReplay = await endpointClient.CallAsync<string, string>("replay", "Why don�t APIs ever get lost? Because they always REST.");
    Console.WriteLine($"Endpoint 'replay' answered with {responseReplay}");

    var responseJson = await endpointClient.CallAsync<Nodes.Device, Nodes.Device>("replay", new Nodes.Device() { Name = "Test Device" });
    Console.WriteLine($"Endpoint 'replay' answered with {responseJson.Name}");
}