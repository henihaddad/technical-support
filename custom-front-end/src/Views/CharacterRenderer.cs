using UID;
using System;
using System.Linq;
using System.Threading.Tasks;
using Mosaik;
using Mosaik.Components;
using Mosaik.Schema;
using Mosaik.Views;
using Tesserae;
using static Tesserae.UI;
using static Mosaik.UI;

using H5;
using static H5.Core.dom;
using Node = Mosaik.Schema.Node;

namespace TechnicalSupport.FrontEnd
{
    public class CharacterRenderer : INodeRenderer
    {
        public string NodeType    => N.Character.Type;
        public string DisplayName => "Character";
        public string LabelField  => "Name";
        public string Color       => "#27ae60";
        public UIcons Icon        => UIcons.User;

        public CardContent CompactView(Node node)
        {
            return CardContent(Header(this, node), null);
        }

        public async Task<CardContent> PreviewAsync(Node node, Parameters state)
        {
            return CardContent(Header(this, node), CreateView(node, state));
        }

        public async Task<IComponent> ViewAsync(Node node, Parameters state)
        {
            return (await PreviewAsync(node, state)).Merge();
        }

        private IComponent CreateView(Node node, Parameters state)
        {
            return Pivot().S().Pivot("overview", PivotTitle("Overview"), () => RenderOverview(node))
                              .Pivot("graph",    PivotTitle("Graph"),    () => RenderGraph(node));
        }

        private IComponent RenderOverview(Node node)
        {
            return VStack().S().Children(
                        Label("Name").WS().Inline().AutoWidth().SetContent(TextBlock(node.GetString(N.Character.Name))),
                        Label("Gender").WS().Inline().AutoWidth().SetContent(TextBlock(node.GetString(N.Character.Gender))),
                        Label("Year of Birth").WS().Inline().AutoWidth().SetContent(TextBlock(node.GetString(N.Character.YearOfBirth))),
                        Label("Year of Death").WS().Inline().AutoWidth().SetContent(TextBlock(node.GetString(N.Character.YearOfDeath))),
                        Label("Species").WS().Inline().AutoWidth().SetContent(NeighborsLinks(node.UID, N.Species.Type, E.CharacterOf).WS()),
                        Label("Organizations").WS().Inline().AutoWidth().SetContent(NeighborsLinks(node.UID, N.Organization.Type, E.MemberOf).WS()));
        }

        private IComponent RenderGraph(Node node)
        {
            return Defer(async () =>
            {
                var queryResult = await Mosaik.API.Query.StartAt(node.UID).Out().TakeAll().GetUIDsAsync();
                return GraphExplorerView.ComponentFor(enableInteraction: true, uids: queryResult.UIDs.Append(node.UID).ToArray()).S();
            }).S();
        }
    }
}
