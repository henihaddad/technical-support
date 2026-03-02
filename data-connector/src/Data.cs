using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalSupport;

public class DeviceJson
{
    public string Name { get; set; }
}

public class PartJson
{
    public string Name { get; set; }
    public string Manufacturer { get; set; }
    public string[] Devices { get; set; }
}

public class SupportCaseJson
{
    public string Summary { get; set; }
    public string Content { get; set; }
    public string Status { get; set; }
    public string Device { get; set; }
    public DateTimeOffset Time { get; set; }
}

public class StapiPageInfo
{
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public bool LastPage { get; set; }
}

public class StapiCharacterResponse
{
    public StapiPageInfo Page { get; set; }
    public StapiCharacter[] Characters { get; set; }
}

public class StapiCharacter
{
    public string Uid { get; set; }
    public string Name { get; set; }
    public string Gender { get; set; }
    public int? YearOfBirth { get; set; }
    public int? YearOfDeath { get; set; }
}

public class StapiCharacterFull
{
    public StapiCharacterDetail Character { get; set; }
}

public class StapiCharacterDetail
{
    public string Uid { get; set; }
    public string Name { get; set; }
    public string Gender { get; set; }
    public int? YearOfBirth { get; set; }
    public int? YearOfDeath { get; set; }
    public StapiRef[] CharacterSpecies { get; set; }
    public StapiRef[] Organizations { get; set; }
}

public class StapiSpeciesResponse
{
    public StapiPageInfo Page { get; set; }
    public StapiSpecies[] Species { get; set; }
}

public class StapiSpecies
{
    public string Uid { get; set; }
    public string Name { get; set; }
    public StapiRef Homeworld { get; set; }
    public bool? HumanoidSpecies { get; set; }
    public bool? ReptilianSpecies { get; set; }
    public bool? TelepathicSpecies { get; set; }
    public bool? WarpCapableSpecies { get; set; }
    public bool? ExtinctSpecies { get; set; }
}

public class StapiOrganizationResponse
{
    public StapiPageInfo Page { get; set; }
    public StapiOrganization[] Organizations { get; set; }
}

public class StapiOrganization
{
    public string Uid { get; set; }
    public string Name { get; set; }
    public bool? Government { get; set; }
    public bool? MilitaryOrganization { get; set; }
    public bool? MedicalOrganization { get; set; }
    public bool? ResearchOrganization { get; set; }
    public bool? SportOrganization { get; set; }
}

public class StapiRef
{
    public string Uid { get; set; }
    public string Name { get; set; }
}