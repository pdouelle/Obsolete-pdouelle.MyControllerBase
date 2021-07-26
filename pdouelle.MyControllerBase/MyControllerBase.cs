using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using pdouelle.Entity;
using pdouelle.GenericMediatR.Models.Generics.Models.Commands.Create;
using pdouelle.GenericMediatR.Models.Generics.Models.Commands.Delete;
using pdouelle.GenericMediatR.Models.Generics.Models.Commands.Patch;
using pdouelle.GenericMediatR.Models.Generics.Models.Commands.Save;
using pdouelle.GenericMediatR.Models.Generics.Models.Commands.Update;
using pdouelle.GenericMediatR.Models.Generics.Models.Queries.IdQuery;
using pdouelle.GenericMediatR.Models.Generics.Models.Queries.ListQuery;

namespace pdouelle.MyControllerBase
{
    public class MyControllerBase<TEntity, TDto, TQueryById> : ControllerBase 
        where TEntity : IEntity
        where TDto : IEntity
        where TQueryById : IEntity, new() 
    {
        protected readonly IMediator Mediator;
        protected readonly IMapper Mapper;

        public MyControllerBase(IMediator mediator, IMapper mapper)
        {
            Mediator = mediator;
            Mapper = mapper;
        }

        /// <summary>
        /// Get all
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        public virtual async Task<IActionResult> GetList<TQueryList>([FromQuery] TQueryList request, CancellationToken cancellationToken) 
        {
            IEnumerable<TEntity> response = await Mediator.Send(new ListQueryModel<TEntity, TQueryList>
            {
                Request = request
            }, cancellationToken);

            var mappedResponse = Mapper.Map<IEnumerable<TDto>>(response);

            return Ok(mappedResponse);
        }

        /// <summary>
        /// Get by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> GetById(Guid id, [FromQuery] TQueryById request, CancellationToken cancellationToken)
        {
            request.Id = id;
            
            TEntity response = await Mediator.Send(new IdQueryModel<TEntity, TQueryById>
            {
                Request = request
            }, cancellationToken);

            if (response == null) return NotFound();

            var mappedResponse = Mapper.Map<TDto>(response);

            return Ok(mappedResponse);
        }
        
        /// <summary>
        /// Create
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status201Created)]
        public virtual async Task<IActionResult> Create<TCreate>([FromBody] TCreate request, CancellationToken cancellationToken)
        {
            TEntity entity = await Mediator.Send(new CreateCommandModel<TEntity, TCreate>
            {
                Request = request
            }, cancellationToken);

            await Mediator.Send(new SaveCommandModel<TEntity>(), cancellationToken);

            var mappedResponse = Mapper.Map<TDto>(entity);

            return CreatedAtAction(nameof(GetById), new {id = mappedResponse.Id}, mappedResponse);
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Update<TUpdate>(Guid id, [FromBody] TUpdate request, CancellationToken cancellationToken) 
        {
            TEntity entity = await Mediator.Send(new IdQueryModel<TEntity, TQueryById>
            {
                Request = new TQueryById {Id = id}
            }, cancellationToken);

            if (entity == null) return NotFound();

            await Mediator.Send(new UpdateCommandModel<TEntity, TUpdate>
            {
                Entity = entity,
                Request = request
            }, cancellationToken);

            await Mediator.Send(new SaveCommandModel<TEntity>(), cancellationToken);

            var mappedResponse = Mapper.Map<TDto>(entity);

            return Ok(mappedResponse);
        }

        /// <summary>
        /// Patch
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patchDocument"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Patch<TPatch>(Guid id, [FromBody] JsonPatchDocument<TPatch> patchDocument, CancellationToken cancellationToken)
            where TPatch : class, new()
        {
            TEntity entity = await Mediator.Send(new IdQueryModel<TEntity, TQueryById>
            {
                Request = new TQueryById {Id = id}
            }, cancellationToken);

            if (entity == null) return NotFound();

            var request = new TPatch();
            patchDocument.ApplyTo(request);

            TEntity response = await Mediator.Send(new PatchCommandModel<TEntity, TPatch>
            {
                Entity = entity,
                Request = request
            }, cancellationToken);

            await Mediator.Send(new SaveCommandModel<TEntity>(), cancellationToken);

            var mappedResponse = Mapper.Map<TDto>(response);

            return Ok(mappedResponse);
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual async Task<IActionResult> Delete<TDelete>(Guid id, [FromQuery] TDelete request, CancellationToken cancellationToken)
        {
            TEntity entity = await Mediator.Send(new IdQueryModel<TEntity, TQueryById>
            {
                Request = new TQueryById {Id = id}
            }, cancellationToken);

            if (entity == null) return NotFound();

            await Mediator.Send(new DeleteCommandModel<TEntity, TDelete>
            {
                Entity = entity,
                Request = request
            }, cancellationToken);

            await Mediator.Send(new SaveCommandModel<TEntity>(), cancellationToken);

            return NoContent();
        }
    }
}