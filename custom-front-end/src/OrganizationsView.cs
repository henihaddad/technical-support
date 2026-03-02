using H5.Core;
using Tesserae;
using static Mosaik.UI;

namespace TechnicalSupport.FrontEnd
{
    internal class OrganizationsView : IComponent
    {
        private IComponent _container;

        public OrganizationsView(Parameters state)
        {
            _container = HubStack(HubTitle("Organizations", "#/organizations"), "#/home")
                            .Section(CreateView(), grow: true);
        }

        private IComponent CreateView()
        {
            return SearchArea().WithFacets().OnSearch(s => s.SetBeforeTypesFacet(N.Organization.Type)).S();
        }

        public dom.HTMLElement Render() => _container.Render();
    }
}
