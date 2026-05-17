using Xunit;

namespace Storylines.Tests.Models
{
    public class DialogueTests
    {
        #region Create

        [Fact]
        public void Create_WithCharacterAndNewParagraph_ReturnsNewLinePrefix()
        {
            var character = new Character { Name = "Alice" };

            var result = Dialogue.Create(character, newParagraph: true);

            Assert.Equal($"{Environment.NewLine}Alice: ", result);
        }

        [Fact]
        public void Create_WithCharacterNoNewParagraph_ReturnsNameColonOnly()
        {
            var character = new Character { Name = "Bob" };

            var result = Dialogue.Create(character, newParagraph: false);

            Assert.Equal("Bob: ", result);
        }

        #endregion

        #region GetInText (legacy structured)

        [Fact]
        public void GetInText_NullInput_ReturnsEmptyList()
        {
            var result = Dialogue.GetInText(null);

            Assert.Empty(result);
        }

        [Fact]
        public void GetInText_NoDialogues_ReturnsEmptyList()
        {
            var result = Dialogue.GetInText("Just some regular text.");

            Assert.Empty(result);
        }

        [Fact]
        public void GetInText_SingleLegacyDialogue_ReturnsMatch()
        {
            var text = "Before {name=Alice; text=\"Hello world\"} after.";

            var result = Dialogue.GetInText(text);

            Assert.Single(result);
            Assert.Contains("Alice", result[0]);
        }

        [Fact]
        public void GetInText_MultipleLegacyDialogues_ReturnsAll()
        {
            var text = "{name=Alice; text=\"Hello\"} normal text {name=Bob; text=\"Hi\"}";

            var result = Dialogue.GetInText(text);

            Assert.Equal(2, result.Count);
        }

        #endregion

        #region GetValuesFromString (legacy structured parsing)

        [Fact]
        public void GetValuesFromString_ParsesNameAndText()
        {
            var text = "{name=Alice; text=\"How are you?\"}";

            var result = Dialogue.GetValuesFromString(text);

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("How are you?", result[0].Text);
        }

        [Fact]
        public void GetValuesFromString_TrimsWhitespace()
        {
            var text = "{name= Alice ; text=\" Hello \"}";

            var result = Dialogue.GetValuesFromString(text);

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("Hello", result[0].Text);
        }

        [Fact]
        public void GetValuesFromString_SkipsEmptyNames()
        {
            var text = "{name=  ; text=\"some text\"}";

            var result = Dialogue.GetValuesFromString(text);

            Assert.Empty(result);
        }

        #endregion

        #region GetFromCharactersFromString

        [Fact]
        public void GetFromCharactersFromString_NullText_ReturnsEmpty()
        {
            var result = Dialogue.GetFromCharactersFromString(null, new List<string> { "Alice" });

            Assert.Empty(result);
        }

        [Fact]
        public void GetFromCharactersFromString_NullCharacters_ReturnsEmpty()
        {
            var result = Dialogue.GetFromCharactersFromString("Alice: Hello", null);

            Assert.Empty(result);
        }

        [Fact]
        public void GetFromCharactersFromString_UnifiedFormat_ParsesCorrectly()
        {
            var text = "Alice: Hello, how are you?";
            var characters = new List<string> { "Alice" };

            var result = Dialogue.GetFromCharactersFromString(text, characters);

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("Hello, how are you?", result[0].Text);
        }

        [Fact]
        public void GetFromCharactersFromString_UnifiedFormat_CaseInsensitive()
        {
            var text = "alice: Hello";
            var characters = new List<string> { "Alice" };

            var result = Dialogue.GetFromCharactersFromString(text, characters);

            Assert.Single(result);
        }

        [Fact]
        public void GetFromCharactersFromString_MultipleCharacters_ParsesBoth()
        {
            var text = "Alice: Hello\nBob: Hi there";
            var characters = new List<string> { "Alice", "Bob" };

            var result = Dialogue.GetFromCharactersFromString(text, characters);

            Assert.Equal(2, result.Count);
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
        }

        [Fact]
        public void GetFromCharactersFromString_UnifiedMultiLine_JoinsText()
        {
            var text = "Alice: Hello\nThis is a continuation.";
            var characters = new List<string> { "Alice" };

            var result = Dialogue.GetFromCharactersFromString(text, characters);

            Assert.Single(result);
            Assert.Contains("continuation", result[0].Text);
        }

        [Fact]
        public void GetFromCharactersFromString_LegacyStructured_WorksToo()
        {
            var text = "{name=Alice; text=\"Legacy hello\"}";
            var characters = new List<string> { "Alice" };

            var result = Dialogue.GetFromCharactersFromString(text, characters);

            Assert.Single(result);
            Assert.Equal("Legacy hello", result[0].Text);
        }

        [Fact]
        public void GetFromCharactersFromString_LegacySimple_ParsesStandaloneName()
        {
            var text = "Alice\nHello world";
            var characters = new List<string> { "Alice" };

            var result = Dialogue.GetFromCharactersFromString(text, characters);

            Assert.Single(result);
            Assert.Equal("Hello world", result[0].Text);
        }

        [Fact]
        public void GetFromCharactersFromString_EmptyCharacters_FiltersToNone()
        {
            var text = "Alice: Hello";
            var characters = new List<string>();

            var result = Dialogue.GetFromCharactersFromString(text, characters);

            Assert.Empty(result);
        }

        [Fact]
        public void GetFromCharactersFromString_WhitespaceOnlyCharacters_Ignored()
        {
            var text = "Alice: Hello";
            var characters = new List<string> { "  ", "" };

            var result = Dialogue.GetFromCharactersFromString(text, characters);

            Assert.Empty(result);
        }

        [Fact]
        public void GetFromCharactersFromString_DuplicateCharacters_Deduplicated()
        {
            var text = "Alice: Hello";
            var characters = new List<string> { "Alice", "alice" };

            var result = Dialogue.GetFromCharactersFromString(text, characters);

            Assert.Single(result);
        }

        #endregion

        #region FormatDialoguesToString

        [Fact]
        public void FormatDialoguesToString_FormatsCorrectly()
        {
            var dialogues = new List<Dialogue>
            {
                new Dialogue { Name = "Alice", Text = "Hello" },
                new Dialogue { Name = "Bob", Text = "Hi" }
            };

            var result = Dialogue.FormatDialoguesToString(dialogues);

            Assert.Contains("ALICE: Hello", result);
            Assert.Contains("BOB: Hi", result);
        }

        [Fact]
        public void FormatDialoguesToString_NullList_ReturnsEmpty()
        {
            var result = Dialogue.FormatDialoguesToString(null);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void FormatDialoguesToString_EmptyList_ReturnsEmpty()
        {
            var result = Dialogue.FormatDialoguesToString(new List<Dialogue>());

            Assert.Equal(string.Empty, result);
        }

        #endregion
    }
}
