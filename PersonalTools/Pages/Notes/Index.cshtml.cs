using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PersonalTools.Classes.Notes;
using PersonalTools.Entities.Notes;

namespace PersonalTools.Pages.Notes
{
    public class IndexModel : PageModel
    {
        private readonly INoteFuncs _noteFuncs;

        public IndexModel(INoteFuncs noteFuncs)
        {
            _noteFuncs = noteFuncs;
        }

        public List<NoteObj> Notes { get; set; } = new();

        [BindProperty]
        public string NoteId { get; set; } = string.Empty;

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string Body { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public async Task OnGet()
        {
            Notes = await _noteFuncs.GetNotes();
        }

        public async Task<IActionResult> OnPostCreate()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                ErrorMessage = "Please enter a note title.";
                Notes = await _noteFuncs.GetNotes();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Body))
            {
                ErrorMessage = "Please enter some note content.";
                Notes = await _noteFuncs.GetNotes();
                return Page();
            }

            await _noteFuncs.CreateNote(Title, Body);

            TempData["SuccessMessage"] = "Note added successfully.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDelete(string noteId)
        {
            if (!string.IsNullOrWhiteSpace(noteId))
            {
                await _noteFuncs.DeleteNote(noteId);
                TempData["SuccessMessage"] = "Note deleted successfully.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEdit()
        {
            if (string.IsNullOrWhiteSpace(NoteId))
            {
                ErrorMessage = "Could not find the note to update.";
                Notes = await _noteFuncs.GetNotes();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                ErrorMessage = "Please enter a note title.";
                Notes = await _noteFuncs.GetNotes();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Body))
            {
                ErrorMessage = "Please enter some note content.";
                Notes = await _noteFuncs.GetNotes();
                return Page();
            }

            await _noteFuncs.UpdateNote(NoteId, Title, Body);

            TempData["SuccessMessage"] = "Note updated successfully.";

            return RedirectToPage();
        }
    }
}