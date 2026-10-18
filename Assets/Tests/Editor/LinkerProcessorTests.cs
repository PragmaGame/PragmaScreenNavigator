using System.Linq;
using NUnit.Framework;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary><see cref="ScreenNavigatorLinkerProcessor"/>: which view models the generated link.xml keeps from stripping.</summary>
    public class LinkerProcessorTests
    {
        [Test]
        public void LinkXml_PreservesConcreteViewModels_UnderTheirLinkerNames()
        {
            var xml = ScreenNavigatorLinkerProcessor.GenerateLinkXml(new[]
            {
                typeof(FirstTestViewModel),
                typeof(LoggedScreenViewModel),
                typeof(NestedViewModel),
            });

            StringAssert.Contains("<assembly fullname=\"Pragma.ScreenNavigator.Tests.Editor\" ignoreIfMissing=\"1\">", xml);
            StringAssert.Contains("<type fullname=\"Pragma.ScreenNavigator.Tests.FirstTestViewModel\" preserve=\"all\"/>", xml);
            StringAssert.Contains("<type fullname=\"Pragma.ScreenNavigator.Tests.LinkerProcessorTests/NestedViewModel\" preserve=\"all\"/>", xml);
            StringAssert.DoesNotContain("LoggedScreenViewModel", xml, "an abstract view model is kept through the concrete ones");
        }

        [Test]
        public void PlayerViewModelTypes_SkipEditorOnlyAssemblies()
        {
            var types = ScreenNavigatorLinkerProcessor.FindPlayerViewModelTypes().ToArray();

            CollectionAssert.Contains(types, typeof(ScreenViewModel), "the package assembly reaches the player");
            CollectionAssert.DoesNotContain(types, typeof(FirstTestViewModel), "the tests never do");
        }

        public sealed class NestedViewModel : ElementViewModel
        {
        }
    }
}
