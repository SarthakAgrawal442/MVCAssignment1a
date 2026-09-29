using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCSampleApp.Models;
using MVCSampleApp.Services;
using AppContext = MVCSampleApp.AppContext;

namespace MVCSampleApp.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly AppContext _context;
        private readonly FieldEncryptionService _enc;
        private readonly AuditService _audit;

        public EmployeesController(
            AppContext context,
            FieldEncryptionService enc,
            AuditService audit)
        {
            _context = context;
            _enc = enc;
            _audit = audit;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Name)
                .Include(e => e.Address)
                .Include(e => e.MyService)
                .ToListAsync();

            foreach (var e in employees)
                DecryptPerson(e);

            return View(employees);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Name)
                .Include(e => e.Address)
                .Include(e => e.MyService)
                .FirstOrDefaultAsync(e => e.ID == id.Value);

            if (employee == null)
                return NotFound();

            DecryptPerson(employee);
            return View(employee);
        }

        // GET: Employees/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.roles = new List<SelectListItem>
            {
                new SelectListItem { Text = "Admin", Value = "1" },
                new SelectListItem { Text = "Staff",  Value = "2" }
            };
            ViewBag.services = _context.Services
                .Select(s => new SelectListItem
                {
                    Text  = s.Name,
                    Value = s.ID.ToString()
                }).ToList();

            return View();
        }

        // POST: Employees/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Address,DateOfBirth,ServiceId,Salary,Roles,ID")] Employee employee)
        {
            if (!ModelState.IsValid)
                return View(employee);

            employee.ID = Guid.NewGuid();
            employee.MyService = await _context.Services
                .FindAsync(employee.ServiceId);

            EncryptPerson(employee);

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                action:   "Employee.Create",
                entityId: employee.ID.ToString(),
                payload:  new { employee.ID },
                summary:  $"Employee created by {User.Identity?.Name}");

            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Name)
                .Include(e => e.Address)
                .FirstOrDefaultAsync(e => e.ID == id.Value);

            if (employee == null)
                return NotFound();

            DecryptPerson(employee);
            return View(employee);
        }

        // POST: Employees/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [Bind("Salary,Roles,ID")] Employee employee)
        {
            if (id != employee.ID)
                return NotFound();

            if (!ModelState.IsValid)
                return View(employee);

            // Load tracked entity; only Salary and Roles are posted.
            var existing = await _context.Employees
                .FirstOrDefaultAsync(e => e.ID == id);

            if (existing == null)
                return NotFound();

            existing.Salary = employee.Salary;
            existing.Roles  = employee.Roles;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                action:   "Employee.Edit",
                entityId: id.ToString(),
                payload:  new { id, employee.Salary, employee.Roles },
                summary:  $"Employee edited by {User.Identity?.Name}");

            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Name)
                .Include(e => e.Address)
                .FirstOrDefaultAsync(e => e.ID == id.Value);

            if (employee == null)
                return NotFound();

            DecryptPerson(employee);
            return View(employee);
        }

        // POST: Employees/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                await _audit.LogAsync(
                    action:   "Employee.Delete",
                    entityId: id.ToString(),
                    payload:  new { id },
                    summary:  $"Employee deleted by {User.Identity?.Name}");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(Guid id) =>
            _context.Employees.Any(e => e.ID == id);

        // ── Encrypt/Decrypt helpers ───────────────────────────────────────

        private void EncryptPerson(Person person)
        {
            if (person.Name != null)
            {
                person.Name.FirstName = _enc.Encrypt(person.Name.FirstName);
                person.Name.LastName  = _enc.Encrypt(person.Name.LastName);
            }

            if (person.Address != null)
            {
                person.Address.Street     = _enc.Encrypt(person.Address.Street);
                person.Address.PostalCode = _enc.Encrypt(person.Address.PostalCode);
            }

            person.DateOfBirth = _enc.EncryptDate(
                string.IsNullOrEmpty(person.DateOfBirth)
                    ? null
                    : DateTime.Parse(person.DateOfBirth));
        }

        private void DecryptPerson(Person person)
        {
            if (person.Name != null)
            {
                person.Name.FirstName = _enc.Decrypt(person.Name.FirstName);
                person.Name.LastName  = _enc.Decrypt(person.Name.LastName);
            }

            if (person.Address != null)
            {
                person.Address.Street     = _enc.Decrypt(person.Address.Street);
                person.Address.PostalCode = _enc.Decrypt(person.Address.PostalCode);
            }

            person.DateOfBirth = _enc.DecryptDate(person.DateOfBirth)
                ?.ToString("yyyy-MM-dd");
        }
    }
}