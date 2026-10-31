using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pragma.ScreenNavigator.Tests
{
    /// <summary>What the UI takes from the game's input: <see cref="PanelExtensions"/> and <see cref="VisualElementExtensions.IsTextInput"/>.</summary>
    public class PanelInputTests
    {
        [Test]
        public void IsTextInput_HoldsForTextFieldsAndTheirParts()
        {
            var textField = new TextField();
            var integerField = new IntegerField();
            var container = new VisualElement();
            container.Add(textField);

            Assert.IsTrue(textField.IsTextInput());
            Assert.IsTrue(textField.Q(className: TextField.inputUssClassName).IsTextInput(), "the inner input holds the focus while typing");
            Assert.IsTrue(integerField.IsTextInput(), "fields of numbers are typed into too");
            Assert.IsFalse(new Button().IsTextInput());
            Assert.IsFalse(container.IsTextInput(), "only the field and what is inside it");
            Assert.IsFalse(((VisualElement)null).IsTextInput());
        }

        [Test]
        public void PanelQueries_WithoutAPanel_TakeNothing()
        {
            IPanel panel = null;
            Assert.IsFalse(panel.IsPointerOverUi(Vector2.zero));
            Assert.IsFalse(panel.IsTyping());
        }
    }
}
