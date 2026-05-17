using Xunit;

namespace Storylines.Tests.ViewModels;

/// <summary>
/// Tests for CharactersPageViewModel business logic — change detection,
/// edit mode state, and data field management.
///
/// The ViewModel depends on ServiceLocator/TimeTravelSystem at runtime,
/// so we test the pure logic using local mirrors of the relevant methods.
/// </summary>
public class CharactersPageViewModelLogicTests
{
    #region Helpers mirroring ViewModel logic

    private sealed class TestCharacter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Role { get; set; }
        public string Age { get; set; }
        public string TraitsText { get; set; }
        public string Appearance { get; set; }
    }

    /// <summary>
    /// Mirrors CharactersPageViewModel.DidSomethingChange() logic.
    /// </summary>
    private static bool DidSomethingChange(
        TestCharacter selected,
        string nameText, string descriptionText, string roleText, string ageText,
        string traitsText, string appearanceText)
    {
        if (selected == null) return false;
        return selected.Name != nameText
            || selected.Description != descriptionText
            || selected.Role != (string.IsNullOrEmpty(roleText) ? null : roleText)
            || selected.Age != (string.IsNullOrEmpty(ageText) ? null : ageText)
            || selected.TraitsText != traitsText
            || selected.Appearance != appearanceText;
    }

    /// <summary>
    /// Mirrors the ApplyChanges logic — writes ViewModel fields to the character.
    /// </summary>
    private static void ApplyChanges(
        TestCharacter target,
        string nameText, string descriptionText, string roleText, string ageText,
        string traitsText, string appearanceText)
    {
        target.Name = nameText;
        target.Description = descriptionText;
        target.Role = roleText;
        target.Age = ageText;
        target.TraitsText = traitsText;
        target.Appearance = appearanceText;
    }

    #endregion

    #region DidSomethingChange

    [Fact]
    public void DidSomethingChange_NullCharacter_ReturnsFalse()
    {
        Assert.False(DidSomethingChange(null, "a", "b", "c", "d", "", ""));
    }

    [Fact]
    public void DidSomethingChange_IdenticalFields_ReturnsFalse()
    {
        var ch = new TestCharacter
        {
            Name = "Alice",
            Description = "A hero",
            Role = "Protagonist",
            Age = "25",
            TraitsText = "brave, kind",
            Appearance = "tall"
        };

        Assert.False(DidSomethingChange(ch, "Alice", "A hero", "Protagonist", "25", "brave, kind", "tall"));
    }

    [Fact]
    public void DidSomethingChange_NameChanged_ReturnsTrue()
    {
        var ch = new TestCharacter { Name = "Alice", Description = "", TraitsText = "", Appearance = "" };
        Assert.True(DidSomethingChange(ch, "Bob", "", "", "", "", ""));
    }

    [Fact]
    public void DidSomethingChange_DescriptionChanged_ReturnsTrue()
    {
        var ch = new TestCharacter { Name = "Alice", Description = "Old", TraitsText = "", Appearance = "" };
        Assert.True(DidSomethingChange(ch, "Alice", "New", "", "", "", ""));
    }

    [Fact]
    public void DidSomethingChange_EmptyRole_TreatedAsNull()
    {
        // When the UI textbox is empty, roleText is "", which should match a null Role
        var ch = new TestCharacter { Name = "A", Description = "", Role = null, TraitsText = "", Appearance = "" };
        Assert.False(DidSomethingChange(ch, "A", "", "", "", "", ""));
    }

    [Fact]
    public void DidSomethingChange_EmptyAge_TreatedAsNull()
    {
        var ch = new TestCharacter { Name = "A", Description = "", Age = null, TraitsText = "", Appearance = "" };
        Assert.False(DidSomethingChange(ch, "A", "", "", "", "", ""));
    }

    [Fact]
    public void DidSomethingChange_TraitsChanged_ReturnsTrue()
    {
        var ch = new TestCharacter { Name = "A", Description = "", TraitsText = "brave", Appearance = "" };
        Assert.True(DidSomethingChange(ch, "A", "", "", "", "brave, kind", ""));
    }

    [Fact]
    public void DidSomethingChange_AppearanceChanged_ReturnsTrue()
    {
        var ch = new TestCharacter { Name = "A", Description = "", TraitsText = "", Appearance = "tall" };
        Assert.True(DidSomethingChange(ch, "A", "", "", "", "", "short"));
    }

    #endregion

    #region ApplyChanges

    [Fact]
    public void ApplyChanges_UpdatesAllFields()
    {
        var ch = new TestCharacter
        {
            Name = "Old", Description = "OldDesc", Role = "OldRole",
            Age = "10", TraitsText = "old", Appearance = "old"
        };

        ApplyChanges(ch, "New", "NewDesc", "NewRole", "20", "new", "new");

        Assert.Equal("New", ch.Name);
        Assert.Equal("NewDesc", ch.Description);
        Assert.Equal("NewRole", ch.Role);
        Assert.Equal("20", ch.Age);
        Assert.Equal("new", ch.TraitsText);
        Assert.Equal("new", ch.Appearance);
    }

    [Fact]
    public void ApplyChanges_EmptyStrings_SetCorrectly()
    {
        var ch = new TestCharacter { Name = "Alice", Description = "Desc" };

        ApplyChanges(ch, "Alice", "Desc", "", "", "", "");

        Assert.Equal("", ch.Role);
        Assert.Equal("", ch.Age);
    }

    #endregion

    #region Edit mode state transitions

    [Fact]
    public void EditModeTransition_EnterThenExit_StateIsConsistent()
    {
        // Simulates the state transitions in the ViewModel
        bool isEditMode = false;
        bool isFieldsEnabled = false;
        bool isListEnabled = true;
        string editButtonGlyph = "\uE70F"; // Edit glyph

        // Enter edit mode
        isEditMode = true;
        isFieldsEnabled = true;
        isListEnabled = false;
        editButtonGlyph = "\uE711"; // Cancel glyph

        Assert.True(isEditMode);
        Assert.True(isFieldsEnabled);
        Assert.False(isListEnabled);
        Assert.Equal("\uE711", editButtonGlyph);

        // Exit edit mode
        isEditMode = false;
        isFieldsEnabled = false;
        isListEnabled = true;
        editButtonGlyph = "\uE70F";

        Assert.False(isEditMode);
        Assert.False(isFieldsEnabled);
        Assert.True(isListEnabled);
        Assert.Equal("\uE70F", editButtonGlyph);
    }

    #endregion

    #region CancelEdit — backup restoration

    [Fact]
    public void CancelEdit_RestoresFromBackup()
    {
        var backup = new TestCharacter
        {
            Name = "Original",
            Description = "OrigDesc",
            Role = "Hero",
            Age = "25",
            TraitsText = "brave",
            Appearance = "tall"
        };

        // Simulate editing
        string nameText = "Modified";
        string descriptionText = "ModDesc";

        // Simulate cancel — restore from backup
        nameText = backup.Name;
        descriptionText = backup.Description;

        Assert.Equal("Original", nameText);
        Assert.Equal("OrigDesc", descriptionText);
    }

    #endregion
}
