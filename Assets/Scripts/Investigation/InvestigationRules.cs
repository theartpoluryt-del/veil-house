using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilHouse {
    public static class InvestigationRules {
        public static StoryCatalog LoadCatalog() {
            TextAsset source=Resources.Load<TextAsset>("storybook");
            if(source==null) throw new InvalidOperationException("Отсутствует Resources/storybook.json");
            var catalog=JsonUtility.FromJson<StoryCatalog>(source.text);
            ValidateCatalog(catalog); return catalog;
        }
        public static void ValidateCatalog(StoryCatalog catalog) {
            if(catalog==null || catalog.categories==null || catalog.categories.Length==0 || catalog.stories==null || catalog.stories.Length==0)
                throw new InvalidOperationException("Каталог историй пуст");
            var categories=new HashSet<string>();
            foreach(var category in catalog.categories) {
                if(category==null || string.IsNullOrWhiteSpace(category.id) || !categories.Add(category.id) || category.options==null || category.options.Length==0)
                    throw new InvalidOperationException("Повторяющаяся или пустая категория");
                var options=new HashSet<string>();
                foreach(var option in category.options) if(option==null || string.IsNullOrWhiteSpace(option.id) || !options.Add(option.id))
                    throw new InvalidOperationException("Повторяющийся или пустой вариант: "+category.id);
            }
            var ids=new HashSet<string>();
            foreach(var story in catalog.stories) {
                if(story==null || string.IsNullOrWhiteSpace(story.id) || !ids.Add(story.id) || story.answers==null || story.answers.Length!=catalog.categories.Length)
                    throw new InvalidOperationException("Неполная или повторяющаяся история");
                var answered=new HashSet<string>();
                foreach(var answer in story.answers) if(answer==null || !answered.Add(answer.categoryId) || !IsValidAnswer(catalog,answer.categoryId,answer.optionId))
                    throw new InvalidOperationException("Неизвестный ответ в истории: "+story.id);
            }
        }
        public static StoryCatalog PublicCatalog(StoryCatalog source) { return new StoryCatalog {categories=source.categories,stories=new DeathStory[0]}; }
        public static bool IsValidAnswer(StoryCatalog catalog,string category,string option) {
            if(catalog?.categories==null) return false;
            foreach(var c in catalog.categories) if(c.id==category) { foreach(var o in c.options) if(o.id==option) return true; return false; }
            return false;
        }
        public static bool Submit(List<JournalEntry> journal,StoryCatalog catalog,string category,string option,float elapsed) {
            if(!IsValidAnswer(catalog,category,option)) return false;
            foreach(var entry in journal) if(entry.categoryId==category) {
                // Re-clicking the same answer must never reset a detective's tie-break time.
                if(entry.optionId!=option) {entry.optionId=option;entry.submittedAt=elapsed;}
                return true;
            }
            journal.Add(new JournalEntry {categoryId=category,optionId=option,submittedAt=elapsed}); return true;
        }
        public static List<ScoreRow> Score(IEnumerable<PlayerState> players,Dictionary<int,List<JournalEntry>> journals,DeathStory story) {
            var rows=new List<ScoreRow>();
            if(story==null) return rows;
            foreach(var player in players) {
                if(player.role!=PlayerRole.Detective) continue;
                List<JournalEntry> journal; if(!journals.TryGetValue(player.id,out journal)) journal=new List<JournalEntry>();
                var row=new ScoreRow {playerId=player.id,name=player.name,entries=journal.ToArray()};
                foreach(var entry in journal) foreach(var correct in story.answers)
                    if(entry.categoryId==correct.categoryId && entry.optionId==correct.optionId) {row.correct++;row.tieTime+=entry.submittedAt;break;}
                rows.Add(row);
            }
            rows.Sort((a,b)=> {int c=b.correct.CompareTo(a.correct); if(c!=0)return c;c=a.tieTime.CompareTo(b.tieTime);return c!=0?c:a.playerId.CompareTo(b.playerId);});
            return rows;
        }
    }
}
