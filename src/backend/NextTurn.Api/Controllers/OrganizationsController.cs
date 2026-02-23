using Microsoft.AspNetCore.Mvc;
using NextTurn.Api.Models;
using NextTurn.Api.Repositories;

namespace NextTurn.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationsController : ControllerBase
    {
        private readonly OrganizationRepository _repo;

        public OrganizationsController(OrganizationRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<List<Organization>>> GetAll()
        {
            var items = await _repo.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Organization>> GetById(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        public record CreateOrganizationDto(string Name);
        public record UpdateOrganizationDto(string Name);

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateOrganizationDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            var newId = await _repo.CreateAsync(dto.Name.Trim());
            var created = await _repo.GetByIdAsync(newId);

            return CreatedAtAction(nameof(GetById), new { id = newId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateOrganizationDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            var updated = await _repo.UpdateAsync(id, dto.Name.Trim());
            if (!updated) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _repo.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}