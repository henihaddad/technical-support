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
    public class OrganizationRenderer : INodeRenderer
    {
        public string NodeType    => N.Organization.Type;
        public string DisplayName => "Organization";
        public string LabelField  => "Name";
        public string Color       => "#e67e22";
        public UIcons Icon        => UIcons.Building;

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
                              .Pivot("members",  PivotTitle("Members"),  () => RenderMembers(node));
        }

        private IComponent RenderOverview(Node node)
        {
            return VStack().S().Children(
                        Label("Name").WS().Inline().AutoWidth().SetContent(TextBlock(node.GetString(N.Organization.Name))),
                        Label("Type").WS().Inline().AutoWidth().SetContent(TextBlock(node.GetString(N.Organization.Description))));
        }

        private IComponent RenderMembers(Node node)
        {
            return Neighbors(() => Mosaik.API.Query.StartAt(node.UID).Out(N.Character.Type, E.HasMember).TakeAll().GetUIDsAsync(),
                             new[] { N.Character.Type }, showSearchBox: true, facetDisplay: FacetDisplayOptions.Visible);
        }
    }
}
