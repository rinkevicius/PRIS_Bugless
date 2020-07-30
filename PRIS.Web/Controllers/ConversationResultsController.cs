﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRIS.Core.Library.Entities;
using PRIS.Web.Mappings;
using PRIS.Web.Models;
using PRIS.Web.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PRIS.Web.Controllers
{
    public class ConversationResultsController : Controller
    {
        private readonly IRepository _repository;
        private readonly ILogger<ConversationResultsController> _logger;
        private readonly string _user;

        public ConversationResultsController(IRepository repository, ILogger<ConversationResultsController> logger, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _logger = logger;
            _user = httpContextAccessor.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name).Value;
        }
        public async Task<IActionResult> Index(int? id)
        {
            TempData["ExamId"] = id;

            var passedStudents = await _repository.Query<Student>()
                .Include(i => i.Result)
                .ThenInclude(u => u.Exam)
                .Where(x => x.PassedExam == true)
                .Where(y => y.Result.Exam.Id == id)
                .ToListAsync();
            var conversationResult = new List<ConversationResult>();
            foreach (var student in passedStudents)
            {
                var p = await _repository.FindByIdAsync<ConversationResult>(student.ConversationResultId);
                conversationResult.Add(p);
            }
            List<StudentViewModel> studentViewModels = new List<StudentViewModel>();
            passedStudents.ForEach(x => studentViewModels.Add(ConversationResultMappings.ToStudentViewModel(x)));
            var exam = await _repository.Query<Exam>().Include(x => x.City).FirstOrDefaultAsync(x => x.Id == id);

            List<ConversationResultViewModel> conversationResultViewModel = new List<ConversationResultViewModel>();
            conversationResult.ForEach(x => conversationResultViewModel.Add(ConversationResultMappings.ToConversationResultViewModel(x)));
            conversationResultViewModel.ForEach(x => x.ExamCityAndDate = $"{exam.City.Name}, {exam.Date.ToShortDateString()}");

            List<ConversationFormViewModel> conversationFormViewModels = new List<ConversationFormViewModel>();


            ConversationResultMappings.ToStudentAndConversationResultViewModel(studentViewModels, conversationResultViewModel, id, conversationFormViewModels);

            return View(ConversationResultMappings.ToStudentAndConversationResultViewModel(studentViewModels, conversationResultViewModel, id, conversationFormViewModels));

        }
        //GET
        public async Task<IActionResult> EditConversationResult(int? id, int? examId)
        {
            int.TryParse(TempData["ExamId"].ToString(), out int ExamId);
            var passedStudents = await _repository.Query<Student>()
                .Include(i => i.Result)
                .ThenInclude(u => u.Exam)
                .Where(x => x.PassedExam == true)
                .Where(y => y.Result.Exam.Id == examId)
                .ToListAsync();

            if (id == null)
            {
                return NotFound();
            }
            var student = await _repository.Query<Student>()
                .Include(x => x.ConversationResult)
                .FirstOrDefaultAsync(x => x.Id == id);

            TempData["ConversationResultId"] = student.ConversationResultId;
            TempData["StudentId"] = student.Id;

            if (student.ConversationResult == null)
            {
                ConversationResult conversationResult = new ConversationResult();
                conversationResult.Student = student;

                student.ConversationResult = conversationResult;
                conversationResult = await _repository.InsertAsync(conversationResult);
                _logger.LogInformation("New conversation result added to DB. User {User}.", _user);
                student.ConversationResultId = conversationResult.Id;
                TempData["ConversationResultId"] = student.ConversationResultId;
                await _repository.SaveAsync();
                ConversationResultViewModel conversationResultViewModel = new ConversationResultViewModel();
                conversationResultViewModel.ConversationResultId = conversationResult.Id;
                TempData["ExamId"] = ExamId;
                examId = ExamId;
                return View(ConversationResultMappings.ToViewModel(student, conversationResult, examId));
            }
            else
            {
                var conversationResult = await _repository.FindByIdAsync<ConversationResult>(student.ConversationResultId);
                ConversationResultViewModel conversationResultViewModel = new ConversationResultViewModel();
                conversationResultViewModel.ConversationResultId = conversationResult.Id;
                TempData["ExamId"] = ExamId;
                examId = ExamId;
                return View(ConversationResultMappings.ToViewModel(student, conversationResult, examId));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConversationResult([Bind("Grade", "ConversationResultComment")] ConversationResultViewModel model)
        {
            int.TryParse(TempData["ConversationResultId"].ToString(), out int conversationResultId);
            int.TryParse(TempData["StudentId"].ToString(), out int studentId);
            int.TryParse(TempData["ExamId"].ToString(), out int examId);
            var studentFromDatabase = await _repository.FindByIdAsync<Student>(studentId);
            if (studentFromDatabase.InvitedToStudy == true)
            {
                _logger.LogWarning("Failed to edit conversation result, student {Student} has invited to study. User {User}", studentFromDatabase.Id, _user);
                ModelState.AddModelError("ConversationResultEdit", "Kandidatas yra pakviestas studijuoti, pokalbio įvertinimo redaguoti negalima");
                TempData["ErrorMessage"] = "Kandidatas yra pakviestas studijuoti, pokalbio įvertinimo redaguoti negalima";
                return RedirectToAction("EditConversationResult", "ConversationResults", new { id = studentId });
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var studentRequest = _repository.Query<Student>().Include(x => x.ConversationResult).Where(x => x.Id > 0);
                    var conversationResult = await _repository.FindByIdAsync<ConversationResult>(conversationResultId);
                    var student = await studentRequest.FirstOrDefaultAsync(x => x.ConversationResultId == conversationResult.Id);
                    var conversationResultViewModel = ConversationResultMappings.ToViewModel(student, conversationResult, examId);
                    ConversationResultMappings.EditEntity(conversationResult, model);
                    await _repository.UpdateAsync(conversationResult);
                    _logger.LogInformation("Successfully edited conversation result {ConversationResult}. User {User}",conversationResult.Id, _user);
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                return RedirectToAction("Index", "ConversationResults", new { id = examId });
            }
            _logger.LogWarning("Bad input, conversation result input must be between 0 and 10 student. User {User}", _user);
            TempData["ErrorMessage"] = "Pokalbio įvertinimas turi būti nuo 0 iki 10";
            ModelState.AddModelError("ConversationResultRange", "Pokalbio įvertinimas turi būti nuo 0 iki 10");
            return RedirectToAction("EditConversationResult", "ConversationResults", new { id = studentId });
        }

        //GET
        public async Task<IActionResult> EditConversationForm(int? id, int? studentId)
        {
            var studentById = await _repository.Query<Student>().Include(x => x.ConversationResult).FirstOrDefaultAsync(x => x.Id == studentId);
            if(studentById.ConversationResult != null)
            id = studentById.ConversationResult.Id;
            int.TryParse(TempData["ExamId"].ToString(), out int examId);
            var conversationForms = await _repository
                .Query<ConversationForm>()
                .Include(x => x.ConversationResult)
                .ThenInclude(x => x.Student)
                .Where(x => x.ConversationResultId == id)
                .ToListAsync();
            List<ConversationForm> newConversationForms = new List<ConversationForm>();
            if (conversationForms.Count() == 0)
            {
                if (studentById.ConversationResult == null)
                { 
                ConversationResult conversationResult = new ConversationResult();
                
                conversationResult.Student = studentById;

                studentById.ConversationResult = conversationResult;
                conversationResult = await _repository.InsertAsync(conversationResult);
                    id = conversationResult.Id;
                }
                for (int i = 0; i < 10; i++)
                {
                    newConversationForms.Add(new ConversationForm { ConversationResultId = id });
                    await _repository.InsertAsync<ConversationForm>(newConversationForms.ElementAt(i));
                }
                conversationForms = newConversationForms;
                conversationForms = await _repository
                    .Query<ConversationForm>()
                    .Include(x => x.ConversationResult)
                    .ThenInclude(x => x.Student)
                    .Where(x => x.ConversationResultId == id)
                    .ToListAsync();
                _logger.LogInformation("Successfully created conversation form. User {User}", _user);
            }
            TempData["ExamId"] = examId;
            var student = conversationForms.Select(x => x.ConversationResult?.Student).FirstOrDefault();
            TempData["ConversationResultId"] = student.ConversationResultId;
            TempData["StudentId"] = student.Id;
            return View(ConversationResultMappings.ToConversationFormViewModel(conversationForms, student, examId));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConversationForm(ConversationFormViewModel model)
        {
            int.TryParse(TempData["ConversationResultId"].ToString(), out int conversationResultId);
            int.TryParse(TempData["ExamId"].ToString(), out int examId);
            int.TryParse(TempData["StudentId"].ToString(), out int studentId);
            var studentFromDatabase = await _repository.FindByIdAsync<Student>(studentId);
            if (studentFromDatabase.InvitedToStudy == true)
            {
                _logger.LogWarning("Failed to edit conversation result form, student {Student} has invited to study. User {User}", studentFromDatabase.Id, _user);
                ModelState.AddModelError("ConversationFormEdit", "Kandidatas yra pakviestas studijuoti, pokalbio anketos redaguoti negalima");
                TempData["ErrorMessage"] = "Kandidatas yra pakviestas studijuoti, pokalbio anketos redaguoti negalima";
                return RedirectToAction("EditConversationForm", "ConversationResults", new { id = conversationResultId });
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var conversationForm = await _repository.Query<ConversationForm>().Where(x => x.ConversationResultId == conversationResultId).ToListAsync();
                    ConversationResultMappings.EditConversationFormEntity(conversationForm, model);
                    _logger.LogInformation("Successfully edited conversation form. User {User}", _user);
                    await _repository.SaveAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                return RedirectToAction("Index", "ConversationResults", new { id = examId });
            }
            return RedirectToAction("EditConversationForm", "ConversationResults", new { id = conversationResultId });


        }
    }
}
