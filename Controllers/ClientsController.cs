using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCSampleApp.Models;
using MVCSampleApp.Services;
using AppContext = MVCSampleApp.AppContext;

namespace MVCSampleApp.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly AppContext _context;
        private readonly FieldEncryptionService _enc;

        public ClientsController(AppContext context, FieldEncryptionService enc)
        {
            _context = context;
            _enc = enc;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            List<Client> clients = await _context.Clients
                .AsNoTracking()
                .Include(c => c.Name)
                .Include(c => c.Address)
                .ToListAsync();

            foreach (var client in clients)
                DecryptClient(client);

            return View(clients);
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var client = await _context.Clients
                .AsNoTracking()
                .Include(c => c.Name)
                .Include(c => c.Address)
                .FirstOrDefaultAsync(c => c.ID == id.Value);

            if (client == null)
                return NotFound();

            DecryptClient(client);
            return View(client);
        }

        // GET: Clients/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clients/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Address,Income,DateOfBirth,ID")] Client client,
            IFormFile? file)
        {
            if (!TryValidateIncome(client.Income, out int income))
                return View(client);

            // The photo is optional.
            if (file != null && file.Length > 0)
            {
                using var target = new MemoryStream();
                await file.CopyToAsync(target);
                client.Photo = target.ToArray();
            }

            client.ID = Guid.NewGuid();
            EncryptClient(client, income);

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Clients/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var client = await _context.Clients
                .AsNoTracking()
                .Include(c => c.Name)
                .Include(c => c.Address)
                .FirstOrDefaultAsync(c => c.ID == id.Value);

            if (client == null)
                return NotFound();

            DecryptClient(client);
            return View(client);
        }

        // POST: Clients/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [Bind("Name,Address,Income,DateOfBirth,ID")] Client client)
        {
            if (id != client.ID)
                return NotFound();

            if (!TryValidateIncome(client.Income, out int income))
                return View(client);

            // Load the existing, tracked entities. Do not Update(client):
            // the posted client is a separate object with new Name/Address objects.
            var existing = await _context.Clients
                .Include(c => c.Name)
                .Include(c => c.Address)
                .FirstOrDefaultAsync(c => c.ID == id);

            if (existing == null)
                return NotFound();

            if (client.Name != null)
            {
                existing.Name ??= new FullName();
                existing.Name.FirstName = _enc.Encrypt(client.Name.FirstName);
                existing.Name.LastName = _enc.Encrypt(client.Name.LastName);
            }

            if (client.Address != null)
            {
                existing.Address ??= new FullAddress();
                existing.Address.Street = _enc.Encrypt(client.Address.Street);
                existing.Address.City = client.Address.City;
                existing.Address.Province = client.Address.Province;
                existing.Address.PostalCode =
                    _enc.Encrypt(client.Address.PostalCode);
            }

            existing.Income = _enc.EncryptInt(income);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Clients/AvailableServices/5
        public async Task<IActionResult> AvailableServices(Guid? id)
        {
            if (id == null)
                return NotFound();

            var services = await _context.Services
                .AsNoTracking()
                .Include(s => s.Clients)
                .Where(s => s.Clients.All(c => c.ID != id.Value))
                .ToListAsync();

            ViewBag.Id = id;
            return View(services);
        }

        // Keep the existing route/link shape for now. These actions change
        // data via GET and should later be changed to POST with antiforgery.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddService(
            int ServiceId,
            Guid ClientId)
        {
            var service = await _context.Services.FindAsync(ServiceId);
            var client = await _context.Clients
                .Include(c => c.Services)
                .FirstOrDefaultAsync(c => c.ID == ClientId);

            if (service == null || client == null)
                return NotFound();

            if (!client.Services.Any(s => s.ID == ServiceId))
            {
                client.Services.Add(service);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Clients/RegisteredServices/5
        public async Task<IActionResult> RegisteredServices(Guid? id)
        {
            if (id == null)
                return NotFound();

            var services = await _context.Services
                .AsNoTracking()
                .Include(s => s.Clients)
                .Where(s => s.Clients.Any(c => c.ID == id.Value))
                .ToListAsync();

            ViewBag.Id = id;
            return View(services);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveService(
            int ServiceId,
            Guid ClientId)
        {
            var client = await _context.Clients
                .Include(c => c.Services)
                .FirstOrDefaultAsync(c => c.ID == ClientId);

            if (client == null)
                return NotFound();

            var service = client.Services
                .FirstOrDefault(s => s.ID == ServiceId);

            if (service == null)
                return NotFound();

            client.Services.Remove(service);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Clients/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var client = await _context.Clients
                .AsNoTracking()
                .Include(c => c.Name)
                .Include(c => c.Address)
                .FirstOrDefaultAsync(c => c.ID == id.Value);

            if (client == null)
                return NotFound();

            DecryptClient(client);
            return View(client);
        }

        // POST: Clients/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TryValidateIncome(string? value, out int income)
        {
            if (!int.TryParse(value, out income) ||
                income < 0 || income > 1000)
            {
                ModelState.AddModelError(
                    nameof(Client.Income),
                    "Income must be a whole number between 0 and 1000.");
            }

            return ModelState.IsValid;
        }

        private void EncryptPerson(Person person)
        {
            if (person.Name != null)
            {
                person.Name.FirstName =
                    _enc.Encrypt(person.Name.FirstName);
                person.Name.LastName =
                    _enc.Encrypt(person.Name.LastName);
            }

            if (person.Address != null)
            {
                person.Address.Street =
                    _enc.Encrypt(person.Address.Street);
                person.Address.PostalCode =
                    _enc.Encrypt(person.Address.PostalCode);
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
                person.Name.FirstName =
                    _enc.Decrypt(person.Name.FirstName);
                person.Name.LastName =
                    _enc.Decrypt(person.Name.LastName);
            }

            if (person.Address != null)
            {
                person.Address.Street =
                    _enc.Decrypt(person.Address.Street);
                person.Address.PostalCode =
                    _enc.Decrypt(person.Address.PostalCode);
            }

            person.DateOfBirth = _enc.DecryptDate(person.DateOfBirth)
                ?.ToString("yyyy-MM-dd");
        }

        private void EncryptClient(Client client, int income)
        {
            EncryptPerson(client);
            client.Income = _enc.EncryptInt(income);
        }

        private void DecryptClient(Client client)
        {
            DecryptPerson(client);

            if (!string.IsNullOrEmpty(client.Income))
                client.Income = _enc.DecryptInt(client.Income).ToString();
        }
    }
}