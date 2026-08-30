namespace PersonalTools.Data.CaseOpening;

/// <summary>
/// Stable game keys mapped to the upstream CSGO-API crate identifiers. Keeping the selection
/// separate from the fetcher makes catalogue reviews small and prevents API ordering changes
/// from changing the simulator's progression catalogue.
/// </summary>
public static class CaseOpeningCatalogue
{
    public static IReadOnlyDictionary<string, string> Containers { get; } = new Dictionary<string, string>
    {
        // Every conventional weapon case currently available from the upstream catalogue.
        ["weapon-case-1"] = "crate-4001", ["esports-2013"] = "crate-4002", ["operation-bravo"] = "crate-4003",
        ["weapon-case-2"] = "crate-4004", ["esports-2013-winter"] = "crate-4005", ["winter-offensive"] = "crate-4009",
        ["weapon-case-3"] = "crate-4010", ["operation-phoenix"] = "crate-4011", ["huntsman"] = "crate-4017",
        ["breakout"] = "crate-4018", ["esports-2014-summer"] = "crate-4019", ["operation-vanguard"] = "crate-4029",
        ["chroma"] = "crate-4061", ["chroma-2"] = "crate-4089", ["falchion"] = "crate-4091",
        ["shadow"] = "crate-4138", ["revolver"] = "crate-4186", ["operation-wildfire"] = "crate-4187",
        ["chroma-3"] = "crate-4233", ["gamma"] = "crate-4236", ["gamma-2"] = "crate-4281",
        ["glove"] = "crate-4288", ["spectrum"] = "crate-4351", ["hydra"] = "crate-4352",
        ["spectrum-2"] = "crate-4403", ["clutch"] = "crate-4471", ["horizon"] = "crate-4482",
        ["danger-zone"] = "crate-4548", ["prisma"] = "crate-4598", ["shattered-web"] = "crate-4620",
        ["cs20"] = "crate-4669", ["prisma-2"] = "crate-4695", ["fracture"] = "crate-4698",
        ["broken-fang"] = "crate-4717", ["snakebite"] = "crate-4747", ["riptide"] = "crate-4790",
        ["dreams-and-nightmares"] = "crate-4818", ["recoil"] = "crate-4846", ["revolution"] = "crate-4880",
        ["kilowatt"] = "crate-4904", ["gallery"] = "crate-7003", ["fever"] = "crate-7007",

        // A deliberately varied capsule history: major legends plus recognisable themed sets.
        ["sticker-capsule-2"] = "crate-4012", ["community-sticker-capsule-1"] = "crate-4016",
        ["cologne-2014-legends"] = "crate-4020",
        ["dreamhack-2014-legends"] = "crate-4030", ["katowice-2015-legends"] = "crate-4086",
        ["enfu"] = "crate-4090", ["cologne-2015-legends"] = "crate-4109", ["cluj-napoca-2015-legends"] = "crate-4156",
        ["pinups"] = "crate-4183", ["team-roles"] = "crate-4185", ["mlg-columbus-2016-legends"] = "crate-4205",
        ["cologne-2016-legends"] = "crate-4254", ["atlanta-2017-legends"] = "crate-4323",
        ["krakow-2017-legends"] = "crate-4391", ["boston-2018-legends"] = "crate-4478",
        ["london-2018-legends"] = "crate-4533", ["katowice-2019-legends"] = "crate-4584", ["halo"] = "crate-4597",
        ["warhammer-40000"] = "crate-4616", ["berlin-2019-legends"] = "crate-4654", ["cs20-stickers"] = "crate-4670",
        ["half-life-alyx"] = "crate-4694", ["rmr-2020-legends"] = "crate-4743", ["poorly-drawn"] = "crate-4746",
        ["stockholm-2021-legends"] = "crate-4803", ["antwerp-2022-legends"] = "crate-4832",
        ["ten-year-birthday"] = "crate-4847", ["rio-2022-legends"] = "crate-4857", ["espionage"] = "crate-4879",
        ["paris-2023-legends"] = "crate-4890", ["ambush"] = "crate-4905", ["copenhagen-2024-legends"] = "crate-4923",
        ["shanghai-2024-legends"] = "crate-4964", ["austin-2025-legends"] = "crate-5117",
        ["budapest-2025-legends"] = "crate-5216", ["warhammer-traitor-astartes"] = "crate-7011",
        ["warhammer-adeptus-astartes"] = "crate-7013", ["feral-predators"] = "crate-4599",
        ["jackass"] = "crate-7041",

        // Keep this family represented with a liquid package that has dependable recent sales.
        ["paris-2023-mirage-souvenir"] = "crate-4894"
    };
}
