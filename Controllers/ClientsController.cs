using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCSampleApp;
using MVCSampleApp.Models;

namespace MVCSampleApp.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly AppContext _context;

        public ClientsController(AppContext context)
        {
            _context = context;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            List<Client> clients = await _context.Clients.Include(x => x.Name).Include(x => x.Address).ToListAsync();
            return View(clients);
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.ID == id);
            if (client == null)
            {
                return NotFound();
            }

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
        public async Task<IActionResult> Create([Bind("Name, Address, Income,ID")] Client client, IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using (var target = new MemoryStream())
                {
                    file.CopyTo(target);
                    client.Photo = target.ToArray();
                }
            }

            if (ModelState.IsValid)
            {
                client.ID = Guid.NewGuid();
                _context.Add(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.Include(x => x.Name).Include(x => x.Address).FirstOrDefaultAsync(x => x.ID == id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }

        // POST: Clients/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Name, Address, Income,ID")] Client client)
        {
            if (id != client.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/AvailableServices/5
        public async Task<IActionResult> AvailableServices(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            List<Service> services = await _context.Services.Include(x => x.Clients).Where(x => x.Clients.All(y => y.ID != id)).ToListAsync();
            ViewBag.Id = id;
            return View(services);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddService(int ServiceId, Guid ClientId)
        {
            Service service = await _context.Services.FindAsync(ServiceId);
            Client client = await _context.Clients.Include(x => x.Services).FirstOrDefaultAsync(y => y.ID == ClientId);
            client.Services.Add(service);
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction("Index");
        }

        // GET: Clients/RegisteredServices/5
        public async Task<IActionResult> RegisteredServices(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            List<Service> services = await _context.Services.Include(x => x.Clients).Where(x => x.Clients.Any(y => y.ID == id)).ToListAsync();
            ViewBag.Id = id;
            return View(services);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveService(int ServiceId, Guid ClientId)
        {
            Service service = await _context.Services.FindAsync(ServiceId);
            Client client = await _context.Clients.Include(x => x.Services).FirstOrDefaultAsync(y => y.ID == ClientId);
            client.Services.Remove(service);
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction("Index");
        }

        // GET: Clients/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.ID == id);
            if (client == null)
            {
                return NotFound();
            }

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
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClientExists(Guid id)
        {
            return _context.Clients.Any(e => e.ID == id);
        }
    }
}