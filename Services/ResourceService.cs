// Classe pour rendre la liste des ressources accessible depuis n'importe quel composant
// (évitant d'avoir à passer le dictionnaire de composant en composant)

using CaptainArchitect.Models;

namespace CaptainArchitect.Services;

public class ResourceService
{
    private readonly Dictionary<string, Resource> _resources = new();

    public void Load(IEnumerable<Resource> resources)
    {
        _resources.Clear();
        foreach (var r in resources)
        {
            _resources[r.Id] = r;
        }
    }

    public Resource? Get(string resourceId) =>
        _resources.TryGetValue(resourceId, out var res) ? res : null;
}