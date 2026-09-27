using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;

namespace BlazorApp1.Models;

public enum PortDirection
{
    Input,
    Output
}

public class ResourcePortModel : PortModel
{
    public string ResourceId { get; }
    public PortDirection Direction { get; }

    public ResourcePortModel(string resourceId, NodeModel parent, PortDirection direction, PortAlignment alignment)
        : base(parent, alignment)
    {
        ResourceId = resourceId;
        Direction = direction;
    }

    // Règle de validation native appelée par Blazor.Diagrams au lâcher de la souris
    public override bool CanAttachTo(ILinkable other)
    {
        // On ne peut se lier qu'à un autre port
        if (other is not ResourcePortModel targetPort)
            return false;

        // Pas de lien sur la même machine
        if (targetPort.Parent == this.Parent)
            return false;

        // Une sortie doit aller vers une entrée (ou inversement)
        if (this.Direction == targetPort.Direction)
            return false;

        // Les deux ports doivent partager exactement la même ressource
        if (!string.Equals(this.ResourceId, targetPort.ResourceId, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}