// Crée une classe qui hérite de LinkModel et transporte le code couleur hexadécimal

using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;

namespace CaptainArchitect.Models;

public class ResourceLinkModel : LinkModel
{
    public string Color { get; set; } = "#38bdf8";

    public ResourceLinkModel(Anchor source, Anchor? target = null)
        : base(source, target)
    {
    }
}