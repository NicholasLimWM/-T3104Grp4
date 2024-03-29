﻿using ICT3104_Group4_SMS.DAL;
using ICT3104_Group4_SMS.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ICT3104_Group4_SMS.Controllers
{
    [Authorize(Roles = "Lecturer,Admin")]
    public class LecturerController : Controller
    {
        private SmsContext db = new SmsContext();
        internal IDataGateway<Lecturer_Module> Lecturer_ModuleGateway;
        internal IDataGateway<ApplicationUser> ApplicationUserGateway;
        internal IDataGateway<Grade> GradesGateway;
        internal IDataGateway<Recommendation> RecommendationGateway;
        private ApplicationUserManager _userManager;

        //internal IDataGateway<Module> ModuleGateway = new DataGateway<Module>();
        private ModuleGateway ModuleGateway = new ModuleGateway();
        private Lecturer_ModuleDataGateway lmDW = new Lecturer_ModuleDataGateway();

        private SmsMapper smsMapper = new SmsMapper();

        // check if user has passed 2FA authentication. true = did not pass. false = passed.
        public bool IfUserSkipTwoFA()
        {
            if (Session == null)
                return true;
            else if (Session["Verified"] == null)
                return true;

            return false;
        }

        public LecturerController()
        {
            Lecturer_ModuleGateway = new Lecturer_ModuleDataGateway();
            ApplicationUserGateway = new ApplicationUserDataGateway();
            GradesGateway = new GradesGateway();
            RecommendationGateway = new RecommendationGateway();
        }

        public LecturerController(ApplicationUserManager userManager)
        {
            Lecturer_ModuleGateway = new Lecturer_ModuleDataGateway();
            ApplicationUserGateway = new ApplicationUserDataGateway();
            GradesGateway = new GradesGateway();
            RecommendationGateway = new RecommendationGateway();

            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        [HttpGet]
        public ActionResult SearchStudentParticulars(string name)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (name != null)
            {
                ViewBag.List = ((ApplicationUserDataGateway)ApplicationUserGateway).searchStudent(name);
            }
            else
            {
                ViewBag.List = ((ApplicationUserDataGateway)ApplicationUserGateway).searchStudent("");
            }
            return View();
        }

        // GET: /Lecturer/EditStudentParticulars
        public ActionResult EditStudentParticulars(string id)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Models.ApplicationUser Applicationuser = db.Users.Find(id);
            if (Applicationuser == null)
            {
                return HttpNotFound();
            }
            return View(Applicationuser);
        }


        // POST: Programmes/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStudentParticulars([Bind(Include = "Id,FullName,UserName,Email,PhoneNumber")] Models.ApplicationUser Applicationuser)
        {
            if (ModelState.IsValid)
            {
                var user = UserManager.FindById(Applicationuser.Id);
                user.FullName = Applicationuser.FullName;
                user.Email = Applicationuser.Email;
                user.PhoneNumber = Applicationuser.PhoneNumber;
                user.UserName = Applicationuser.UserName;
                UserManager.Update(user);

                //db.Entry(Applicationuser).State = EntityState.Modified;
                //db.SaveChanges();
                return RedirectToAction("SearchStudentParticulars");
            }
            return View(Applicationuser);
        }

        // GET: /Lecturer/DeleteStudent
        public ActionResult DeleteStudent(string id)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Models.ApplicationUser Applicationuser = db.Users.Find(id);
            if (Applicationuser == null)
            {
                return HttpNotFound();
            }
            return View(Applicationuser);
        }

        [HttpPost, ActionName("DeleteStudent")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Models.ApplicationUser Applicationuser = db.Users.Find(id);
            db.Users.Remove(Applicationuser);
            db.SaveChanges();
            return RedirectToAction("SearchStudentParticulars");
        }

        // GET: /Lecturer/ModuleTeach
        public ActionResult ModuleTeach()
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            var userID = User.Identity.GetUserId();
            return View(lmDW.selectModuleByLecturer(userID));
        }


        // GET: /Lecturer/GradeAssign
        public ActionResult GradeAssign(int? id)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // lmDW.getModuleStudent(id);
            IEnumerable<Grade> gradeList = lmDW.selectStudentByModule(id);

            if (gradeList.Count() == 0)
            {
                var module1 = ModuleGateway.SelectById(id);
                ViewBag.selectedModule = module1;
                return View(gradeList);
            }
            List<Grade> gList = new List<Grade>(); ;
            List<String> studNameList = new List<string>();
            if (gradeList.Count() != 0)
            {

                foreach (var g in gradeList)
                {
                    var user = (from u in db.Users
                                where u.Id == (g.studentId)
                                select u).FirstOrDefault();
                    if (user != null)
                    {
                        studNameList.Add(user.FullName);
                        gList.Add(g);
                    }

                }
            }

            var module = ModuleGateway.SelectById(id);
            ViewBag.selectedModule = module;
            ViewBag.nameList = studNameList;
            return View(gList);

        }

        [HttpPost]
        public ActionResult GradeAssign(List<Grade> list, string moduleId)
        {
            int? modId = Convert.ToInt32(moduleId);
            if (ModelState.IsValid && list != null)
            {

                foreach (var i in list)
                {
                    var c = db.Grades.Where(a => a.Id.Equals(i.Id)).FirstOrDefault();
                    if (c != null)
                    {
                        c.lecturermoduleId = i.lecturermoduleId;
                        c.score = i.score;
                        c.studentId = i.studentId;
                    }

                }

                Module module = ModuleGateway.SelectById(modId);
                module.status = "Assigned";
                if (ModelState.IsValid)
                {
                    ModuleGateway.Update(module);
                }

                db.SaveChanges();
                TempData["GradesAssigned"] = true;

                //return View("GradeAssign(" + modId+")");
                return RedirectToAction("GradesView", new { id = moduleId, moduleName = module.name });

            }
            else
            {
                TempData["GradesAssigned"] = false;
                String moduleName = ((ModuleGateway)ModuleGateway).SelectById(modId).name;
                return RedirectToAction("GradesView", new { id = moduleId, moduleName = moduleName });
                //return View(list);
            }

        }

        // GET: Grades
        public ActionResult GradesView(int? id, String moduleName)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.moduleId = id;
            ViewBag.moduleName = moduleName;
            if (moduleName == null)
                ViewBag.moduleName = ModuleGateway.GetModuleName(id);

            ViewBag.moduleStatus = ModuleGateway.GetModuleStatus(id);

            int id2 = id ?? default(int);
            IEnumerable<GradeRecViewModel> gradeWithRecList = smsMapper.GradeWithRec(id2);
            if (gradeWithRecList.Select(m => m.RecItem).Where(m => m.status == "Pending").Count() == 0)
                ViewBag.pendingRec = false;
            else
                ViewBag.pendingRec = true;
            return View(gradeWithRecList);
        }

        // GET: Grades
        [HttpGet]
        public ActionResult GradesUpdate(int? id, int recId, int moduleId)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Grade gradeItem = GradesGateway.SelectById(id);
            Recommendation recItem = RecommendationGateway.SelectById(recId);

            ViewBag.RecItem = recItem;
            ViewBag.moduleId = moduleId;
            ViewBag.moduleName = ModuleGateway.SelectById(moduleId).name;
            ViewBag.studentName = UserManager.FindById(gradeItem.studentId).FullName;

            return View(gradeItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GradesUpdate([Bind(Include = "Id,score")] Grade model, int recId, int moduleId)
        {
            //int? modId = Convert.ToInt32(moduleId);
            if (ModelState.IsValid)
            {
                var c = db.Grades.Where(a => a.Id.Equals(model.Id)).FirstOrDefault();
                if (c != null)
                {
                    c.score = model.score;
                }

                db.SaveChanges();
                TempData["GradeUpdateSuccess"] = true;

                //return View("GradeAssign(" + modId+")");
                return RedirectToAction("GradesView/" + moduleId);

            }
            else
            {
                TempData["GradeUpdateSuccess"] = false;
                return RedirectToAction("GradesUpdate", new { model.Id, recId, moduleId });
                //return View(list);
            }

        }
       

        // GET: Modules
        public ActionResult ModuleIndex()
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            return View(db.Modules.ToList());
        }


        // GET: Modules/Create
        public ActionResult ModuleCreate()
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            //ViewBag.List = ((ProgrammeDataGateway)ProgrammeGateway).GetAllProgrammes();

            return View();
        }

        // POST: Modules/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ModuleCreate([Bind(Include = "Id,name,year")] Module module)
        {
            if (ModelState.IsValid)
            {

                module.year = Int32.Parse(DateTime.Now.Year.ToString());
                module.status = "Created";
                db.Modules.Add(module);
                db.SaveChanges();
                //get the id of the inserted module
                int id = module.Id;
                Lecturer_Module lmModel = new Lecturer_Module();
                lmModel.lecturerId = User.Identity.GetUserId();
                lmModel.moduleId = id;
                //insert to Lecturer_Model
                ((Lecturer_ModuleDataGateway)Lecturer_ModuleGateway).Insert(lmModel);
                return RedirectToAction("ModuleIndex");
            }

            return View(module);
        }

        // GET: Modules/Edit/5
        public ActionResult ModuleEdit(int? id)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Module module = db.Modules.Find(id);
            if (module == null)
            {
                return HttpNotFound();
            }
            return View(module);
        }

        // POST: Modules/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ModuleEdit([Bind(Include = "Id,name, status, frozenDateTime, publishDateTime, year")] Module module)
        {
            if (ModelState.IsValid)
            {
                db.Entry(module).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ModuleIndex");
            }
            return View(module);
        }

        // GET: Modules/Delete/5
        public ActionResult ModuleDelete(int? id)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Module module = db.Modules.Find(id);
            if (module == null)
            {
                return HttpNotFound();
            }
            return View(module);
        }

        // POST: Modules/Delete/5
        [HttpPost, ActionName("ModuleDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult MoudleDeleteConfirmed(int id)
        {
            Module module = db.Modules.Find(id);
            db.Modules.Remove(module);
            db.SaveChanges();
            return RedirectToAction("ModuleIndex");
        }

        // GET: Recommendations/Create
        public ActionResult RecommendationCreate(int? id, string name)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            Grade gradeItem = GradesGateway.SelectById(id);
            ViewBag.gradeItem = gradeItem;
            ViewBag.moduleId = lmDW.GetModuleIdFromLecModId(gradeItem.lecturermoduleId);
            ViewBag.name = name;

            return View();
        }

        // POST: Recommendations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RecommendationCreate([Bind(Include = "Id,gradeId, recommendation, status")] Recommendation rec)
        {
            if (ModelState.IsValid)
            {
                rec.lecturerId = User.Identity.GetUserId();
                RecommendationGateway.Insert(rec);
                TempData["Success"] = 1;            // for success message on recommendationedit
                return RedirectToAction("RecommendationEdit", new { id = rec.Id, gradeId = rec.gradeId });
            }

            Grade gradeItem = GradesGateway.SelectById(rec.gradeId);
            ViewBag.gradeItem = gradeItem;
            ViewBag.moduleId = lmDW.GetModuleIdFromLecModId(gradeItem.lecturermoduleId);

            var student = UserManager.FindById(gradeItem.studentId);
            ViewBag.name = student.FullName;

            return View(rec);
        }

        // GET: Recommendations/Edit/5
        public ActionResult RecommendationEdit(int? id, int gradeId)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Recommendation rec = RecommendationGateway.SelectById(id);

            if (rec == null)
            {
                return HttpNotFound();
            }

            Grade gradeItem = GradesGateway.SelectById(gradeId);
            var student = UserManager.FindById(gradeItem.studentId);

            ViewBag.gradeItem = gradeItem;
            ViewBag.moduleId = lmDW.GetModuleIdFromLecModId(gradeItem.lecturermoduleId);
            ViewBag.name = student.FullName;
            return View(rec);
        }

        // POST: Recommendations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RecommendationEdit([Bind(Include = "Id, gradeId, recommendation, status")] Recommendation rec)
        {
            if (ModelState.IsValid)
            {
                rec.lecturerId = User.Identity.GetUserId();
                RecommendationGateway.Update(rec);
                TempData["Success"] = 2;                    // for success message on recommendationedit
                return RedirectToAction("RecommendationEdit", new { gradeId = rec.gradeId });
            }

            Grade gradeItem = GradesGateway.SelectById(rec.gradeId);
            var student = UserManager.FindById(gradeItem.studentId);

            ViewBag.gradeItem = gradeItem;
            ViewBag.moduleId = lmDW.GetModuleIdFromLecModId(gradeItem.lecturermoduleId);
            ViewBag.name = student.FullName;

            return View(rec);
        }


        // GET: StudentEnrolment
        public ActionResult StudentEnrolment()
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            var userID = User.Identity.GetUserId();
            return View(lmDW.selectModuleByLecturer(userID));
        }

        // GET: Grades
        public ActionResult StudentEnrolView(int? id, String moduleName)
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.moduleId = id;
            ViewBag.moduleName = moduleName;

            IEnumerable<Grade> gradeList = lmDW.selectStudentByModule(id);
            List<string[]> studentList = ((ApplicationUserDataGateway)ApplicationUserGateway).searchAllStudent();
            List<string[]> studentNotInModuleList = new List<string[]>();

            foreach (var student in studentList)
            {
                bool found = false;
                foreach (var grade in gradeList)
                {
                    if (student.ElementAt(0).Equals(grade.studentId))
                    {
                        found = true;
                    }
                }
                if (found == false)
                {
                    studentNotInModuleList.Add(student);
                }
            }
            ViewBag.ListStudentNotInModule = studentNotInModuleList;
            ViewBag.selectedEnrolModule = moduleName;

            return View();
        }

        // GET: Grades
        public ActionResult StudentEnrol(string studentId, string moduleName)
        {
            int moduleId = 0;

            if (studentId.Length != 0)
            {
                Grade newGrade = new Grade();
                newGrade.studentId = studentId;
                IEnumerable<Module> modList = ModuleGateway.SelectAll();
                
                foreach (var i in modList)
                {
                    if (i.name.Equals(moduleName))
                    {
                        IEnumerable<Lecturer_Module> lecModList = Lecturer_ModuleGateway.SelectAll();
                        foreach (var j in lecModList)
                        {
                            if (j.moduleId == i.Id)
                            {
                                newGrade.lecturermoduleId = j.Id;
                                moduleId = j.moduleId;
                            }
                        }
                    }
                }
                GradesGateway.Insert(newGrade);
                TempData["StudentEnrolDone"] = true;
            }
            
            return RedirectToAction("StudentEnrolView", new { id = moduleId, moduleName });
        }

        // GET: Grades
        public ActionResult LockGrade(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Module module = ModuleGateway.SelectById(id);
            module.status = "Locked";
            ModuleGateway.Update(module);
            ViewBag.moduleStatus = "Locked";
        
            return RedirectToAction("GradesView", new { module.Id, module.name });

        }
        public ActionResult StudentGPA()
        {
            // check if user has passed 2FA verification. if no, redirect to login page
            if (IfUserSkipTwoFA())
                return RedirectToAction("LoginNonStudent", "Account", new { ReturnUrl = Request.Url.PathAndQuery });

            var userID = User.Identity.GetUserId();
            IEnumerable<Module> LecModule = lmDW.selectModuleByLecturer(userID);
            List<Module> moduleList = db.Modules.ToList();
            List<Module> publish = new List<Module>();
            foreach (var ml in moduleList)
            {

                if (ml.status.Equals("Published"))
                {

                    if (LecModule.Any(x => x.Id == ml.Id))
                    {
                        publish.Add(ml);
                    }
                }
            }
            List<ApplicationUser> userList = new List<ApplicationUser>();

            foreach (var p in publish)
            {
                IEnumerable<Grade> gradeList = lmDW.selectStudentByModule(p.Id);
                foreach (var gl in gradeList)
                {
                    Models.ApplicationUser Auser = db.Users.Find(gl.studentId);
                    if (Auser != null && Auser.GPA != null)
                    {
                        if (!userList.Contains(Auser))
                        {
                            String gpa = Auser.GPA;
                            String decrypt = Decrypt(gpa, Auser.encryptionKey);
                            Auser.GPA = decrypt;

                            userList.Add(Auser);
                        }
                    }
                }
            }

            return View(userList);

        }

        internal static string Decrypt(string input, string key)
        {

            byte[] inputArray = Convert.FromBase64String(input);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(key);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();

            return UTF8Encoding.UTF8.GetString(resultArray);

        }

    }
}




