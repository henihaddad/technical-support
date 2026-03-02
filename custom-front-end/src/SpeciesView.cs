using H5.Core;
using Tesserae;
using static Mosaik.UI;

namespace TechnicalSupport.FrontEnd
{
    internal class SpeciesView : IComponent
    {
        private IComponent _container;

        public SpeciesView(Parameters state)
        {
            _container = HubStack(HubTitle("Species", "#/species"), "#/home")
                            .Section(CreateView(), grow: true);
        }

        private IComponent CreateView()
        {
            return SearchArea().WithFacets().OnSearch(s => s.SetBeforeTypesFacet(N.Species.Type)).S();
        }

        public dom.HTMLElement Render() => _container.Render();
    }
}
