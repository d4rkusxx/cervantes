﻿using Cervantes.Contracts;
using Cervantes.CORE;
using Cervantes.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using Cervantes.IFR.Email;
using Ganss.XSS;

namespace Cervantes.Web.Controllers;
[Authorize(Roles = "Admin,SuperUser,User,Client")]
public class ProjectController : Controller
{
    private readonly ILogger<ProjectController> _logger = null;
    private readonly IHostingEnvironment _appEnvironment;
    private IProjectManager projectManager = null;
    private IClientManager clientManager = null;
    private IProjectUserManager projectUserManager = null;
    private IProjectNoteManager projectNoteManager = null;
    private IProjectAttachmentManager projectAttachmentManager = null;
    private ITargetManager targetManager = null;
    private ITaskManager taskManager = null;
    private IUserManager userManager = null;
    private IVulnManager vulnManager = null;
    private IReportManager reportManager = null;
    private IReportTemplateManager reportTemplateManager = null;
    private IEmailService email = null;

    /// <summary>
    /// ProjectController Constructor
    /// </summary>
    /// <param name="projectManager">ProjectManager</param>
    /// <param name="clientManager">ClientManager</param>
    public ProjectController(IProjectManager projectManager, IClientManager clientManager,
        IProjectUserManager projectUserManager, IProjectNoteManager projectNoteManager,
        IProjectAttachmentManager projectAttachmentManager, ITargetManager targetManager, ITaskManager taskManager,
        IUserManager userManager, IVulnManager vulnManager, IHostingEnvironment _appEnvironment,
        ILogger<ProjectController> logger, IReportManager reportManager, IReportTemplateManager reportTemplateManager,IEmailService email)
    {
        this.projectManager = projectManager;
        this.clientManager = clientManager;
        this.projectUserManager = projectUserManager;
        this.projectNoteManager = projectNoteManager;
        this.projectAttachmentManager = projectAttachmentManager;
        this.targetManager = targetManager;
        this.taskManager = taskManager;
        this.userManager = userManager;
        this.vulnManager = vulnManager;
        this._appEnvironment = _appEnvironment;
        _logger = logger;
        this.reportManager = reportManager;
        this.reportTemplateManager = reportTemplateManager;
        this.email = email;
    }

    /// <summary>
    /// Method Index show all projects
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Admin,SuperUser,User")]
    public ActionResult Index()
    {
        try
        {
            var model = projectManager.GetAll().Where(x => x.Template == false).Select(e => new ProjectViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Client = e.Client,
                ProjectType = e.ProjectType,
                Status = e.Status,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                ClientId = e.ClientId,
                Language = e.Language
            });

            if (model != null)
            {
                return View(model);
            }
            else
            {
                TempData["empty"] = "No clients introduced";
                return View();
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = "loading project";
            _logger.LogError(ex, "An error ocurred adding loading Projects. User: {0}",
                User.FindFirstValue(ClaimTypes.Name));
            return View();
        }
    }

    /// <summary>
    /// Methos show project details
    /// </summary>
    /// <param name="id">Project Id</param>
    /// <returns></returns>
    [Authorize(Roles = "Admin,SuperUser,User,Client")]
    public ActionResult Details(Guid id)
    {
        try
        {
            
            var project = projectManager.GetById(id);
            
           
            
            if (project != null)
            {
                
                if(User.FindFirstValue(ClaimTypes.Role) == "Client")
                {
                    var user = userManager.GetByUserId(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    if (project.ClientId == user.ClientId)
                    {
                        var result2 = userManager.GetAll().Select(e => new ApplicationUser
                        {
                            Id = e.Id,
                            FullName = e.FullName
                        }).ToList();

                        var users2 = new List<SelectListItem>();

                        foreach (var item in result2)
                            users2.Add(new SelectListItem {Text = item.FullName, Value = item.Id.ToString()});
                        
                        var repTemplates = reportTemplateManager.GetAll().Where(x => x.Language == project.Language).Select(e => new ReportTemplate
                        {
                            Id = e.Id,
                            Name = e.Name,
                        }).ToList();

                        var liRep = new List<SelectListItem>();

                        foreach (var item in repTemplates) liRep.Add(new SelectListItem {Text = item.Name, Value = item.Id.ToString()});

                        var model2 = new ProjectDetailsViewModel
                        {
                            Project = project,
                            ProjectUsers = projectUserManager.GetAll().Where(x => x.ProjectId == id).ToList(),
                            ProjectNotes = projectNoteManager.GetAll().Where(x => x.ProjectId == id),
                            ProjectAttachments = projectAttachmentManager.GetAll().Where(x => x.ProjectId == id),
                            Targets = targetManager.GetAll().Where(x => x.ProjectId == id),
                            Tasks = taskManager.GetAll().Where(x => x.ProjectId == id),
                            Users = users2.ToList(),
                            Vulns = vulnManager.GetAll().Where(x => x.ProjectId == id).ToList(),
                            Reports = reportManager.GetAll().Where(x => x.ProjectId == id).ToList(),
                            ReportTemplates = liRep.ToList()
                        };
                        return View(model2);
                    }
                    else
                    {
                        return RedirectToAction("MyProjects");
                    }
                }
                
                var result = userManager.GetAll().Select(e => new ApplicationUser
                {
                    Id = e.Id,
                    FullName = e.FullName
                }).ToList();

                var users = new List<SelectListItem>();

                foreach (var item in result)
                    users.Add(new SelectListItem {Text = item.FullName, Value = item.Id.ToString()});
                
                var repTemplates2 = reportTemplateManager.GetAll().Where(x => x.Language == project.Language).Select(e => new ReportTemplate
                {
                    Id = e.Id,
                    Name = e.Name,
                }).ToList();

                var liRep2 = new List<SelectListItem>();

                foreach (var item in repTemplates2) liRep2.Add(new SelectListItem {Text = item.Name, Value = item.Id.ToString()});

                var model = new ProjectDetailsViewModel
                {
                    Project = project,
                    ProjectUsers = projectUserManager.GetAll().Where(x => x.ProjectId == id).ToList(),
                    ProjectNotes = projectNoteManager.GetAll().Where(x => x.ProjectId == id),
                    ProjectAttachments = projectAttachmentManager.GetAll().Where(x => x.ProjectId == id),
                    Targets = targetManager.GetAll().Where(x => x.ProjectId == id),
                    Tasks = taskManager.GetAll().Where(x => x.ProjectId == id),
                    Users = users.ToList(),
                    Vulns = vulnManager.GetAll().Where(x => x.ProjectId == id).ToList(),
                    Reports = reportManager.GetAll().Where(x => x.ProjectId == id).ToList(),
                    ReportTemplates = liRep2.ToList()
                };
                return View(model);
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["error"] = "loading project";
            _logger.LogError(ex, "An error ocurred loading Project: {0}. User: {1}", id,
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Method sheo create form
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Admin,SuperUser")]
    public ActionResult Create()
    {
        try
        {
            var result = clientManager.GetAll().Select(e => new ClientViewModel
            {
                Id = e.Id,
                Name = e.Name
            }).ToList();

            var li = new List<SelectListItem>();

            foreach (var item in result) li.Add(new SelectListItem {Text = item.Name, Value = item.Id.ToString()});

            var model = new ProjectViewModel
            {
                ItemList = li
            };

            return View(model);
        }
        catch (Exception ex)
        {
            TempData["error"] = "loading form";
            _logger.LogError(ex, "An error ocurred loading Project Create Form. User: {0}",
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Method save reate form
    /// </summary>
    /// <param name="model">ProjectViewModel</param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperUser")]
    public ActionResult Create(ProjectViewModel model)
    {
        try
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedSchemes.Add("data");

            var item = model.ItemList;
            

            var project = new Project
            {
                Name = model.Name,
                Description = sanitizer.Sanitize(HttpUtility.HtmlDecode(model.Description)),
                StartDate = model.StartDate.ToUniversalTime().AddDays(1),
                EndDate = model.EndDate.ToUniversalTime().AddDays(1),
                ClientId = model.ClientId,
                ProjectType = model.ProjectType,
                Status = model.Status,
                Template = model.Template,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Language = model.Language,
                Score = model.Score
            };
            projectManager.Add(project);
            projectManager.Context.SaveChanges();
            TempData["created"] = "created";
            _logger.LogInformation("User: {0} Created a new Project: {1}", User.FindFirstValue(ClaimTypes.Name),
                project.Name);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["error"] = "creating project";
            _logger.LogError(ex, "An error ocurred adding a new Project. User: {0}",
                User.FindFirstValue(ClaimTypes.Name));
            return View();
        }
    }

    /// <summary>
    /// Method show edit form
    /// </summary>
    /// <param name="id">Project Id</param>
    /// <returns></returns>
    [Authorize(Roles = "Admin,SuperUser")]
    public ActionResult Edit(Guid id)
    {
        try
        {
            var result = clientManager.GetAll().Select(e => new ClientViewModel
            {
                Id = e.Id,
                Name = e.Name
            }).ToList();

            var li = new List<SelectListItem>();

            foreach (var item in result) li.Add(new SelectListItem {Text = item.Name, Value = item.Id.ToString()});


            var project = projectManager.GetById(id);
            var model = new ProjectViewModel
            {
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Template = project.Template,
                Client = project.Client,
                ClientId = project.ClientId,
                Status = project.Status,
                Language = project.Language,
                ProjectType = project.ProjectType,
                ItemList = li
            };
            return View(model);
        }
        catch (Exception ex)
        {
            TempData["error"] = "loading project";
            _logger.LogError(ex, "An error ocurred loading Edit Form. User: {0}", User.FindFirstValue(ClaimTypes.Name));
            RedirectToAction("Index");
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Method save edit form
    /// </summary>
    /// <param name="id"></param>
    /// <param name="collection"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperUser")]
    public ActionResult Edit(Guid id, ProjectViewModel model)
    {
        try
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedSchemes.Add("data");

            var result = projectManager.GetById(id);
            result.Name = model.Name;
            result.Status = model.Status;
            result.Description = sanitizer.Sanitize(HttpUtility.HtmlDecode(model.Description));
            result.Template = model.Template;
            result.ClientId = model.ClientId;
            result.Language = model.Language;
            result.StartDate = model.StartDate.ToUniversalTime();
            result.EndDate = model.EndDate.ToUniversalTime();
            result.ProjectType = model.ProjectType;
            projectManager.Context.SaveChanges();

            _logger.LogInformation("User: {0} Edited project. Id: {1}", User.FindFirstValue(ClaimTypes.Name), id);
            return RedirectToAction("Details", "Project", new {id = id});
        }
        catch (Exception ex)
        {
            TempData["error"] = "editing project";
            _logger.LogError(ex, "An error ocurred editing project Id: {0}. User: {1}", id,
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = id});
        }
    }

    // GET: ProjectController/Delete/5
    [Authorize(Roles = "Admin,SuperUser")]
    public ActionResult Delete(Guid id)
    {
        try
        {
            var model = projectManager.GetById(id);

            return View(model);
        }
        catch (Exception ex)
        {
            TempData["error"] = "loading project";
            _logger.LogError(ex, "An error loading delete form. Project Id: {0}. User: {1}", id,
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: ProjectController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperUser")]
    public ActionResult Delete(Guid id, IFormCollection collection)
    {
        try
        {
            var project = projectManager.GetById(id);
            if (project != null)
            {
                projectManager.Remove(project);
                projectManager.Context.SaveChanges();
            }

            _logger.LogInformation("User: {0} deleted Project Id: {1}", User.FindFirstValue(ClaimTypes.Name), id);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["error"] = "deleting ";
            _logger.LogError(ex, "An error ocurred deleting Project Id: {0}. User: {1}", id,
                User.FindFirstValue(ClaimTypes.Name));
            return View();
        }
    }

    /// <summary>
    /// Method show all template projects
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Admin,SuperUser")]
    public ActionResult Templates()
    {
        try
        {
            var model = projectManager.GetAll().Where(x => x.Template == true).Select(e => new ProjectViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Client = e.Client,
                ProjectType = e.ProjectType,
                Status = e.Status,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                ClientId = e.ClientId
            });
            return View(model);
        }
        catch (Exception ex)
        {
            TempData["error"] = "loading templates ";
            _logger.LogError(ex, "An error ocurred loading Project Templates. User: {0}",
                User.FindFirstValue(ClaimTypes.Name));
            return View();
        }
    }

    /// <summary>
    /// Method retrieve project template by id
    /// </summary>
    /// <param name="id">Project Id</param>
    /// <returns></returns>
    [Authorize(Roles = "Admin,SuperUser")]
    public ActionResult Template(Guid id)
    {
        try
        {
            var result = clientManager.GetAll().Select(e => new ClientViewModel
            {
                Id = e.Id,
                Name = e.Name
            }).ToList();

            var li = new List<SelectListItem>();

            foreach (var item in result) li.Add(new SelectListItem {Text = item.Name, Value = item.Id.ToString()});


            var project = projectManager.GetById(id);
            var model = new ProjectViewModel
            {
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Template = project.Template,
                Client = project.Client,
                ClientId = project.ClientId,
                Status = project.Status,
                ProjectType = project.ProjectType,
                ItemList = li
            };
            return View(model);
        }
        catch (Exception ex)
        {
            TempData["error"] = "loading template ";
            _logger.LogError(ex, "An error ocurred loading template Project Id: {0}. User: {1}", id,
                User.FindFirstValue(ClaimTypes.Name));
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperUser")]
    public ActionResult Template(Guid id, ProjectViewModel model)
    {
        try
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedSchemes.Add("data");

            var result = projectManager.GetById(id);
            result.Name = model.Name;
            result.Status = model.Status;
            result.Description = sanitizer.Sanitize(HttpUtility.HtmlDecode(model.Description));
            result.Template = model.Template;
            result.ClientId = model.ClientId;
            result.StartDate = model.StartDate.ToUniversalTime();
            result.EndDate = model.EndDate.ToUniversalTime();
            result.ProjectType = model.ProjectType;
            result.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            projectManager.Add(result);
            projectManager.Context.SaveChanges();

            _logger.LogInformation("User: {0} created project. Id: {1}", User.FindFirstValue(ClaimTypes.Name), id);
            return RedirectToAction("Details", "Project", new {id = id});
        }
        catch (Exception ex)
        {
            TempData["error"] = "creating ";
            _logger.LogError(ex, "An error ocurred creating project Id: {0}. User: {1}", id,
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = id});
        }
    }


    /// <summary>
    /// Method Add user to project
    /// </summary>
    /// <param name="project">Project Id</param>
    /// <param name="user">User Id</param>
    /// <returns></returns>
    [Authorize(Roles = "Admin,SuperUser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddMember(Guid project, string users)
    {
        try
        {
            if (project != null && users != null)
            {
                var result = projectUserManager.GetAll().Where(x => x.UserId == users && x.ProjectId == project);

                if (result.FirstOrDefault() == null)
                {
                    var user = new ProjectUser
                    {
                        ProjectId = project,
                        UserId = users
                    };
                    projectUserManager.Add(user);
                    projectUserManager.Context.SaveChanges();
                    TempData["addedMember"] = "added";
                    _logger.LogInformation("User: {0} Added new member Member on Project: {2}",
                        User.FindFirstValue(ClaimTypes.Name), project);

                    var userRes = userManager.GetByUserId(user.UserId);
                    var projectRes = projectManager.GetById(user.ProjectId);
                    
                    var to = new List<EmailAddress>();
                    to.Add(new EmailAddress
                    {
                        Address = userRes.Email,
                        Name = userRes.FullName,
                    });

                    StreamReader sr =new StreamReader(_appEnvironment.WebRootPath + "/Resources/Email/AddedToProject.html");
                    string s = sr.ReadToEnd();
                    s = s.Replace("{UserName}", userRes.FullName).
                        Replace("{Project}",projectRes.Name).
                        Replace("{StartDate}", projectRes.StartDate.ToShortDateString()).
                        Replace("{EndDate}", projectRes.EndDate.ToShortDateString()).
                        Replace("{Client}",projectRes.Client.Name).
                        Replace("{CervantesLink}",HttpContext.Request.Scheme.ToString() +"://" + HttpContext.Request.Host.ToString() + "/en/Project/Details/"+project);
                    sr.Close();
                
                    EmailMessage message = new EmailMessage
                    {
                        ToAddresses = to,
                        Subject = "Cervantes - You have been added to a Project",
                        Content = s
                    };
             
                    email.Send(message);
                    return RedirectToAction("Details", "Project", new {id = project});
                }
                else
                {
                    TempData["existsMember"] = "exists";
                    return RedirectToAction("Details", "Project", new {id = project});
                }
            }
            else
            {
                return RedirectToAction("Details", "Project", new {id = project});
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = "adding member to ";
            _logger.LogError(ex, "An error ocurred adding a new Memeber on Project: {0}. User: {1}", project,
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = project});
        }
    }

    /// <summary>
    /// Method delete user from project
    /// </summary>
    /// <param name="project">Project Id</param>
    /// <param name="user">User Id</param>
    /// <returns></returns>
    [Authorize(Roles = "Admin,SuperUser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteMember(Guid project, string user)
    {
        try
        {
            var result = projectUserManager.GetAll().Where(x => x.UserId == user && x.ProjectId == project);
            if (result.FirstOrDefault() != null)
            {
                projectUserManager.Remove(result.FirstOrDefault());
                projectUserManager.Context.SaveChanges();
                TempData["deletedMember"] = "deleted";
                return RedirectToAction("Details", "Project", new {id = project});
            }

            return RedirectToAction("Details", "Project", new {id = project});
        }
        catch (Exception ex)
        {
            TempData["error"] = "deleting memeber from ";
            _logger.LogError(ex, "An error ocurred deleteing a Memeber on Project: {0}. User: {1}", project,
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = project});
        }
    }

    [Authorize(Roles = "Admin,SuperUser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddTarget(IFormCollection form)
    {
        try
        {
            if (form != null)
            {
                var target = new Target
                {
                    Name = form["name"],
                    Description = form["description"],
                    ProjectId = Guid.Parse(form["project"]),
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    Type = (TargetType) Enum.ToObject(typeof(TargetType), int.Parse(form["TargetType"]))
                };

                targetManager.Add(target);
                targetManager.Context.SaveChanges();
                TempData["addedTarget"] = "added";
                _logger.LogInformation("User: {0} Added new target: {1} on Project: {2}",
                    User.FindFirstValue(ClaimTypes.Name), target.Name, form["project"]);
                return RedirectToAction("Details", "Project", new {id = form["project"]});
            }
            else
            {
                return RedirectToAction("Details", "Project", new {id = form["project"]});
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = "adding target to ";
            _logger.LogError(ex, "An error ocurred adding a new Target on Project: {0}. User: {1}",
                form["project"], User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = form["project"]});
        }
    }

    [Authorize(Roles = "Admin,SuperUser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteTarget(Guid target, Guid project)
    {
        try
        {
            if (target != null)
            {
                var result = targetManager.GetById(target);

                targetManager.Remove(result);
                targetManager.Context.SaveChanges();
                TempData["deletedTarget"] = "deleted";
                return RedirectToAction("Details", "Project", new {id = project});
            }
            else
            {
                return RedirectToAction("Details", "Project", new {id = project});
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = "deleting target from ";
            _logger.LogError(ex, "An error ocurred deleting a Target: {0} on Project: {1}. User: {2}", project, target,
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = project});
        }
    }

    [Authorize(Roles = "Admin,SuperUser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddNote(IFormCollection form)
    {
        try
        {
            if (form != null)
            {
                var note = new ProjectNote
                {
                    Name = form["noteName"],
                    Description = form["noteDescription"],
                    ProjectId = Guid.Parse(form["project"]),
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    Visibility = (Visibility) Enum.ToObject(typeof(Visibility), int.Parse(form["Visibility"]))
                };

                projectNoteManager.Add(note);
                projectNoteManager.Context.SaveChanges();
                TempData["addedNote"] = "added";
                _logger.LogInformation("User: {0} Added new note: {1} on Project: {2}",
                    User.FindFirstValue(ClaimTypes.Name), note.Name, form["project"]);
                return RedirectToAction("Details", "Project", new {id = form["project"]});
            }
            else
            {
                return RedirectToAction("Details", "Project", new {id = form["project"]});
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = "adding note to ";
            _logger.LogError(ex, "An error ocurred adding a Note on Project: {1}. User: {2}",
                form["project"], User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = form["project"]});
        }
    }

    [Authorize(Roles = "Admin,SuperUser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteNote(Guid id, Guid project)
    {
        try
        {
            if (id != null)
            {
                var result = projectNoteManager.GetById(id);

                projectNoteManager.Remove(result);
                projectNoteManager.Context.SaveChanges();
                TempData["deletedNote"] = "deleted";
                _logger.LogInformation("User: {0} Deleted note: {1} on Project: {2}",
                    User.FindFirstValue(ClaimTypes.Name), result.Name, project);
                return RedirectToAction("Details", "Project", new {id = project});
            }
            else
            {
                return RedirectToAction("Details", "Project", new {id = project});
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = "deleting note from ";
            _logger.LogError(ex, "An error ocurred deleting a Note on Project: {1}. User: {2}", project,
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = project});
        }
    }


    [Authorize(Roles = "Admin,SuperUser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddAttachment(IFormCollection form, IFormFile upload)
    {
        try
        {
            if (form != null && upload != null)
            {
                var file = Request.Form.Files["upload"];
                var uploads = Path.Combine(_appEnvironment.WebRootPath, "Attachments/Project/" + form["project"] + "/");
                var uniqueName = Guid.NewGuid().ToString() + "_" + file.FileName;

                if (Directory.Exists(uploads))
                {
                    using (var fileStream = new FileStream(Path.Combine(uploads, uniqueName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                }
                else
                {
                    Directory.CreateDirectory(uploads);

                    using (var fileStream = new FileStream(Path.Combine(uploads, uniqueName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                }


                var note = new ProjectAttachment
                {
                    Name = form["attachmentName"],
                    ProjectId = Guid.Parse(form["project"]),
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    FilePath = "/Attachments/Project/" + form["project"] + "/" + uniqueName
                };

                projectAttachmentManager.Add(note);
                projectAttachmentManager.Context.SaveChanges();
                TempData["addedAttachment"] = "added";
                _logger.LogInformation("User: {0} Added an attachment: {1} on Project: {2}",
                    User.FindFirstValue(ClaimTypes.Name), note.Name, form["project"]);
                return RedirectToAction("Details", "Project", new {id = form["project"]});
            }
            else
            {
                TempData["errorAttachment"] = "added";
                _logger.LogError("An error ocurred adding an Attachment on Project: {1}. User: {2}",
                    form["project"], User.FindFirstValue(ClaimTypes.Name));
                return RedirectToAction("Details", "Project", new {id = form["project"]});
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = "adding attachment to ";
            _logger.LogError(ex, "An error ocurred adding an Attachment on Project: {1}. User: {2}",
                form["project"], User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = form["project"]});
        }
    }

    [Authorize(Roles = "Admin,SuperUser")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAttachment(Guid id, Guid project)
    {
        try
        {
            if (id != null)
            {
                var result = projectAttachmentManager.GetById(id);

                var pathFile = _appEnvironment.WebRootPath + result.FilePath;
                if (System.IO.File.Exists(pathFile)) System.IO.File.Delete(pathFile);

                projectAttachmentManager.Remove(result);
                projectAttachmentManager.Context.SaveChanges();
                TempData["deletedAttachment"] = "deleted";
                _logger.LogInformation("User: {0} Deleted an attachment: {1} on Project: {2}",
                    User.FindFirstValue(ClaimTypes.Name), result.Name, project);
                return RedirectToAction("Details", "Project", new {id = project});
            }
            else
            {
                _logger.LogError("An error ocurred deleting an Attachment on Project: {1}. User: {2}", project,
                    User.FindFirstValue(ClaimTypes.Name));
                return RedirectToAction("Details", "Project", new {id = project});
            }
        }
        catch (Exception ex)
        {
            TempData["error"] = "deleting attachment from ";
            _logger.LogError(ex, "An error ocurred deleting an Attachment on Project: {1}. User: {2}", project,
                User.FindFirstValue(ClaimTypes.Name));
            return RedirectToAction("Details", "Project", new {id = project});
        }
    }

    [Authorize(Roles = "Admin,SuperUser,User,Client")]
    public ActionResult Download(Guid id)
    {
        var attachment = projectAttachmentManager.GetById(id);

        var filePath = Path.Combine(_appEnvironment.WebRootPath, attachment.FilePath);

        var fileBytes = System.IO.File.ReadAllBytes(filePath);

        return File(fileBytes, attachment.Name);
    }
    
    /// <summary>
    /// Method show project for a client
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Client")]
    public ActionResult MyProjects()
    {
        try
        {
            var user = userManager.GetByUserId(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var model = projectManager.GetAll().Where(x => x.Template == false && x.ClientId == user.ClientId).Select(e => new ProjectViewModel
            {
                Id = e.Id,
                Name = e.Name,
                Client = e.Client,
                ProjectType = e.ProjectType,
                Status = e.Status,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                ClientId = e.ClientId
            });
            
            return View(model);

        }
        catch (Exception ex)
        {
            TempData["error"] = "loading project";
            _logger.LogError(ex, "An error ocurred adding loading Projects. User: {0}",
                User.FindFirstValue(ClaimTypes.Name));
            return View();
        }
    }
}