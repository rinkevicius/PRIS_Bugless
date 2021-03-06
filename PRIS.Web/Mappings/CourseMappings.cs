﻿using PRIS.Core.Library.Entities;
using PRIS.Web.Models.CourseModels;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PRIS.Web.Mappings
{
    public class CourseMappings
    {
        public static StudentEvaluationViewModel ToViewModel(Student student, ConversationResult conversationResult, IEnumerable<StudentCourse> studentCourse, Result result)
        {
            double? finalAverageGrade = 0;
            double? finalTestPoints = JsonSerializer.Deserialize<double[]>(result.Tasks).Sum(x => x);
            double? maxPoints = JsonSerializer.Deserialize<double[]>(result.Exam.Tasks).Sum(x => x);
            double? percentageGrade = finalTestPoints * 100 / maxPoints;
            if (percentageGrade == null || conversationResult == null)
                finalAverageGrade = null;
            else finalAverageGrade = (finalTestPoints + conversationResult.Grade) / 2;
            if (studentCourse.Count() > 0)
                studentCourse = studentCourse.Where(q => q.Priority != null);

            return new StudentEvaluationViewModel
            {
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                FinalTestPoints = finalTestPoints,
                PercentageGrade = percentageGrade,
                ConversationGrade = conversationResult?.Grade,
                FinalAverageGrade = finalAverageGrade,
                Priority = studentCourse.Count() >= 1 ? studentCourse?.FirstOrDefault(x => x?.Priority == 1).Course?.Title : null,
                Priority2 = studentCourse.Count() >= 2 ? studentCourse?.FirstOrDefault(x => x?.Priority == 2).Course?.Title : null,
                Priority3 = studentCourse.Count() >= 3 ? studentCourse?.FirstOrDefault(x => x?.Priority == 3).Course?.Title : null,
                CityId = result?.Exam.City.Id,
                ExamId = result?.Exam.Id,
                CourseId = studentCourse.Count() >= 1 ? studentCourse?.FirstOrDefault(x => x?.Priority == 1).Course?.Id : null,
            };
        }
        public static StudentEvaluationListViewModel ToViewModel(List<StudentEvaluationViewModel> studentEvaluations)
        {
            return new StudentEvaluationListViewModel
            {
                StudentEvaluations = studentEvaluations
            };

        }
        public static StudentLockDataViewModel StudentLockDataToViewModel(Student student, ConversationResult conversationResult, IEnumerable<StudentCourse> studentCourse, Result result)
        {
            double? finalAverageGrade = 0;
            double? finalTestPoints = JsonSerializer.Deserialize<double[]>(result.Tasks).Sum(x => x);
            double? maxPoints = JsonSerializer.Deserialize<double[]>(result.Exam.Tasks).Sum(x => x);
            double? percentageGrade = finalTestPoints * 100 / maxPoints;
            if (percentageGrade == null || conversationResult == null)
                finalAverageGrade = null;
            else finalAverageGrade = (finalTestPoints + conversationResult.Grade) / 2;

            return new StudentLockDataViewModel
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                FinalTestPoints = finalTestPoints,
                PercentageGrade = percentageGrade,
                ConversationGrade = conversationResult?.Grade,
                FinalAverageGrade = finalAverageGrade,
                Priority = studentCourse.Count() >= 1 ? studentCourse?.FirstOrDefault(x => x?.Priority == 1).Course?.Title : null,
                SignedAContract = student.SignedAContract,
                InvitedToStudy = student.InvitedToStudy,
                StudentDataLocked = student.StudentDataLocked,
                CityId = result?.Exam.City.Id,
                ExamId = result?.Exam.Id,
                CourseId = studentCourse.Count() >= 1 ? studentCourse?.FirstOrDefault(x => x?.Priority == 1).Course?.Id : null
            };
        }

        public static StudentLockDataListViewModel StudentLockDataListToViewModel(List<StudentLockDataViewModel> studentLockDatas)
        {
            return new StudentLockDataListViewModel
            {
                StudentDataLocking = studentLockDatas
            };
        }
    }
}
