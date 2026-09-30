using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Anchors;

namespace BlazorApp1.Models;

public class MachineNodeModel : NodeModel
{
    public Recipe Recipe { get; }
    public decimal Multiplier { get; set; } = 1.0m;

    // Débits réels calculés (Clé = ResourcePortModel.Id)
    public Dictionary<string, decimal> ActualInputRates { get; } = new();
    public Dictionary<string, decimal> DistributedOutputRates { get; } = new();
    public Dictionary<string, decimal> DownstreamDemandRates { get; } = new();
        public decimal GetDownstreamDemandRate(string portId) =>
        DownstreamDemandRates.TryGetValue(portId, out var val) ? val : 0m;

    // Taux de fonctionnement effectif (entre 0.0 et 1.0)
    public decimal OperationalEfficiency { get; set; } = 1.0m;

    // Production réelle effective bridée par le manque d'ingrédients
    public decimal GetActualProducedRatePerMin(RecipeItem item) =>
        GetOutputRatePerMin(item) * OperationalEfficiency;


    public MachineNodeModel(Recipe recipe, Point? position = null) : base(position)
    {
        Recipe = recipe;
        Title = recipe.Name;

        if (recipe.Time == null || recipe.Time <= 0)
        {
            Console.Error.WriteLine($"[Avertissement] La recette '{recipe.Id}' ({recipe.Name}) n'a pas de durée valide (Time: {recipe.Time}).");
        }

        // Entrées à gauche : on passe l'ID, le parent (this), et l'alignement
        foreach (var input in recipe.Inputs)
        {
            //AddPort(new PortModel(input.ResourceId, this, PortAlignment.Left));
            AddPort(new ResourcePortModel(input.ResourceId, this, PortDirection.Input, PortAlignment.Left));
        }

        // Sorties à droite : on passe l'ID, le parent (this), et l'alignement
        foreach (var output in recipe.Outputs)
        {
            //AddPort(new PortModel(output.ResourceId, this, PortAlignment.Right));
            AddPort(new ResourcePortModel(output.ResourceId, this, PortDirection.Output, PortAlignment.Right));
        }

    }


    // Débit par minute (60 secondes) = (Quantité / Durée) * 60 * Multiplicateur
    public decimal GetInputRatePerMin(RecipeItem item)
    {
        if (Recipe.Time is null or <= 0)
            return 0m;

        return (item.Amount / Recipe.Time.Value) * 60m * Multiplier;
    }

    public decimal GetOutputRatePerMin(RecipeItem item)
    {
        if (Recipe.Time is null or <= 0)
            return 0m;

        return (item.Amount / Recipe.Time.Value) * 60m * Multiplier;
    }

    public decimal GetActualInputRate(string portId) =>
        ActualInputRates.TryGetValue(portId, out var val) ? val : 0m;

    public decimal GetDistributedOutputRate(string portId) =>
        DistributedOutputRates.TryGetValue(portId, out var val) ? val : 0m;


    // méthode pour obtenir le débit de base unitaire(Multiplier = 1)
    public decimal GetBaseInputRatePerMin(RecipeItem item)
    {
        if (Recipe.Time is null or <= 0) return 0m;
        return (item.Amount / Recipe.Time.Value) * 60m;
    }

    public decimal GetBaseOutputRatePerMin(RecipeItem item)
    {
        if (Recipe.Time is null or <= 0) return 0m;
        return (item.Amount / Recipe.Time.Value) * 60m;
    }

}