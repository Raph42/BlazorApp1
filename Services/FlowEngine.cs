// Parcourt les liaisons du diagramme et propage les flux :

using Blazor.Diagrams;
using Blazor.Diagrams.Core;
using Blazor.Diagrams.Core.Anchors;
using BlazorApp1.Models;

namespace BlazorApp1.Services;

public static class FlowEngine
{
    public static void RecalculateFlows(BlazorDiagram diagram)
    {
        var machineNodes = diagram.Nodes.OfType<MachineNodeModel>().ToList();

        // 1. Réinitialiser les débits entrants et sortants distribués
        foreach (var node in machineNodes)
        {
            node.ActualInputRates.Clear();
            node.DistributedOutputRates.Clear();
            node.DownstreamDemandRates.Clear();

            foreach (var port in node.Ports.OfType<ResourcePortModel>())
            {
                if (port.Direction == PortDirection.Input)
                    node.ActualInputRates[port.Id] = 0m;
                else
                {
                    node.DistributedOutputRates[port.Id] = 0m;
                    node.DownstreamDemandRates[port.Id] = 0m;
                }
            }
        }

        // 2. Pour chaque port de sortie, regarder les liens connectés vers des entrées
        foreach (var sourceNode in machineNodes)
        {
            for (int i = 0; i < sourceNode.Recipe.Outputs.Count; i++)
            {
                var outputItem = sourceNode.Recipe.Outputs[i];
                var outputPort = sourceNode.Ports.OfType<ResourcePortModel>()
                                           .FirstOrDefault(p => p.Direction == PortDirection.Output && p.ResourceId == outputItem.ResourceId);

                if (outputPort == null) continue;

                decimal outputProduction = sourceNode.GetOutputRatePerMin(outputItem);

                // Récupérer tous les ports cibles connectés à cette sortie
                var connectedTargetPorts = new List<ResourcePortModel>();

                foreach (var link in outputPort.Links)
                {
                    // Trouver le port à l'autre bout du lien
                    ResourcePortModel? otherPort = null;
                    if (link.Source is SinglePortAnchor srcAnchor && srcAnchor.Port == outputPort)
                    {
                        if (link.Target is SinglePortAnchor tgtAnchor)
                            otherPort = tgtAnchor.Port as ResourcePortModel;
                    }
                    else if (link.Target is SinglePortAnchor tgtAnchor2 && tgtAnchor2.Port == outputPort)
                    {
                        if (link.Source is SinglePortAnchor srcAnchor2)
                            otherPort = srcAnchor2.Port as ResourcePortModel;
                    }

                    if (otherPort != null && otherPort.Direction == PortDirection.Input)
                    {
                        connectedTargetPorts.Add(otherPort);
                    }
                }

                if (connectedTargetPorts.Count == 0) continue;

                // Calculer la demande totale des cibles
                decimal totalDemand = 0m;
                var targetDemands = new Dictionary<ResourcePortModel, decimal>();

                foreach (var targetPort in connectedTargetPorts)
                {
                    if (targetPort.Parent is MachineNodeModel targetNode)
                    {
                        var reqItem = targetNode.Recipe.Inputs.FirstOrDefault(inp => inp.ResourceId == targetPort.ResourceId);
                        decimal demand = reqItem != null ? targetNode.GetInputRatePerMin(reqItem) : 0m;
                        targetDemands[targetPort] = demand;
                        totalDemand += demand;
                    }
                }

                // Mémoriser la demande totale requise en aval
                sourceNode.DownstreamDemandRates[outputPort.Id] = totalDemand;

                // Répartition du débit
                decimal distributedTotal = 0m;
                foreach (var targetPort in connectedTargetPorts)
                {
                    decimal demand = targetDemands[targetPort];
                    decimal flow = 0m;

                    if (totalDemand > 0)
                    {
                        if (outputProduction >= totalDemand)
                        {
                            flow = demand; // Alimentation à 100%
                        }
                        else
                        {
                            // Sous-alimentation au prorata
                            flow = outputProduction * (demand / totalDemand);
                        }
                    }

                    if (targetPort.Parent is MachineNodeModel targetNode)
                    {
                        targetNode.ActualInputRates[targetPort.Id] += flow;
                    }
                    distributedTotal += flow;
                }

                sourceNode.DistributedOutputRates[outputPort.Id] = distributedTotal;
            }
        }

        // Notifier les nœuds pour le rafraîchissement graphique
        foreach (var node in machineNodes)
        {
            node.Refresh();
        }
    }
}