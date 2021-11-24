using Aqua.Profile;
using BeauUtil;

namespace Aqua.Modeling
{
    public class ModelState {
        public ModelPhases Phase;

        public BestiaryDesc Environment;
        public SiteSurveyData SiteData;
        
        public readonly ConceptualModelState Conceptual = new ConceptualModelState();
    }
}