using PersonalTools.Data.Local;
using PersonalTools.Entities.Notes;

namespace PersonalTools.Classes.Notes
{
    public interface INoteFuncs
    {
        Task<List<NoteObj>> GetNotes();
        Task CreateNote(string title, string body);
        Task UpdateNote(string noteId, string title, string body);
        Task DeleteNote(string noteId);
    }

    public class NoteFuncs : INoteFuncs
    {
        private const string FileName = "notes.json";

        private readonly ILocalJsonData _localJsonData;

        public NoteFuncs(ILocalJsonData localJsonData)
        {
            _localJsonData = localJsonData;
        }

        public async Task<List<NoteObj>> GetNotes()
        {
            List<NoteObj> notes = await _localJsonData.LoadList<NoteObj>(FileName);

            return notes
                .OrderByDescending(x => x.Updated)
                .ToList();
        }

        public async Task CreateNote(string title, string body)
        {
            List<NoteObj> notes = await _localJsonData.LoadList<NoteObj>(FileName);

            NoteObj note = new NoteObj
            {
                NoteId = Guid.NewGuid().ToString(),
                Title = title.Trim(),
                Body = body.Trim(),
                Created = DateTime.Now,
                Updated = DateTime.Now
            };

            notes.Add(note);

            await _localJsonData.SaveList(FileName, notes);
        }

        public async Task DeleteNote(string noteId)
        {
            List<NoteObj> notes = await _localJsonData.LoadList<NoteObj>(FileName);

            NoteObj? note = notes.FirstOrDefault(x => x.NoteId == noteId);

            if (note == null)
            {
                return;
            }

            notes.Remove(note);

            await _localJsonData.SaveList(FileName, notes);
        }

        public async Task UpdateNote(string noteId, string title, string body)
        {
            List<NoteObj> notes = await _localJsonData.LoadList<NoteObj>(FileName);

            NoteObj? note = notes.FirstOrDefault(x => x.NoteId == noteId);

            if (note == null)
            {
                return;
            }

            note.Title = title.Trim();
            note.Body = body.Trim();
            note.Updated = DateTime.Now;

            await _localJsonData.SaveList(FileName, notes);
        }
    }
}