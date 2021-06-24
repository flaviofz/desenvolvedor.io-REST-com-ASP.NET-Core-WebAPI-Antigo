using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using DevIO.Api.ViewModels;
using DevIO.Business.Intefaces;
using DevIO.Business.Models;
using Microsoft.AspNetCore.Mvc;

namespace DevIO.Api.Controllers
{
    [Route("api/[controller]")]
    public  class FornecedoresController : MainController
    {
        private readonly IMapper _mapper;
        private readonly IFornecedorRepository _fornecedorRespository;
        private readonly IEnderecoRepository _enderecoRespository;
        private readonly IFornecedorService _fornecedorService;

        public FornecedoresController(
            IMapper mapper,
            INotificador notificador,
            IFornecedorRepository fornecedorRespository,
            IEnderecoRepository enderecoRespository,
            IFornecedorService fornecedorService) : base(notificador)
        {
            _mapper = mapper;
            _fornecedorRespository = fornecedorRespository;
            _enderecoRespository = enderecoRespository;
            _fornecedorService = fornecedorService;            
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FornecedorViewModel>>> ObterTodos()
        {
            var fornecedores = _mapper.Map<IEnumerable<FornecedorViewModel>>(await _fornecedorRespository.ObterTodos());

            return CustomResponse(fornecedores);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<IEnumerable<FornecedorViewModel>>> ObterPorId(Guid id)
        {
            var fornecedor = await FornecedorProdutosEndereco(id);

            if (fornecedor == null) return NotFound();

            return CustomResponse(fornecedor);
        }

        [HttpGet("obter-endereco/{id:guid}")]
        public async Task<ActionResult<EnderecoViewModel>> ObterEnderecoPorId(Guid id)
        {
            return CustomResponse(_mapper.Map<EnderecoViewModel>(await _enderecoRespository.ObterEnderecoPorFornecedor(id)));
        }        

        [HttpPost]
        public async Task<ActionResult<FornecedorViewModel>> Adicionar(FornecedorViewModel fornecedorViewModel)
        {
            if(!ModelState.IsValid) return CustomResponse(ModelState);

            await _fornecedorService.Adicionar(_mapper.Map<Fornecedor>(fornecedorViewModel));

            return CustomResponse(fornecedorViewModel);
        }

        [HttpPut("{id:guid}")] // A partir do 2.1 já entende que está recebendo da rota e do body
        public async Task<ActionResult<FornecedorViewModel>> Atualizar(Guid id, FornecedorViewModel fornecedorViewModel)
        {
            if (id != fornecedorViewModel.Id)
            {
                NotificarErro("O Id da query é diferente do Id do body");
                return CustomResponse(fornecedorViewModel);
            }

            if(!ModelState.IsValid) return CustomResponse(ModelState);

            await _fornecedorService.Atualizar(_mapper.Map<Fornecedor>(fornecedorViewModel));

            return CustomResponse(fornecedorViewModel);
        }

        [HttpPut("atualizar-endereco/{id:guid}")]
        public async Task<ActionResult<EnderecoViewModel>> AtualizarEndereco(Guid id, EnderecoViewModel enderecoViewModel)
        {
            if (id != enderecoViewModel.Id)
            {
                NotificarErro("O Id da query é diferente do Id do body");
                return CustomResponse(enderecoViewModel);
            }

            if(!ModelState.IsValid) return CustomResponse(ModelState);

            var endereco = _mapper.Map<Endereco>(enderecoViewModel);
            await _fornecedorService.AtualizarEndereco(endereco);

            return CustomResponse(enderecoViewModel);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<FornecedorViewModel>> Excluir(Guid id)
        {
            var fornecedorViewModel = await FornecedorEndereco(id);

            if (fornecedorViewModel == null) return NotFound();

            await _fornecedorService.Remover(id);

            return CustomResponse(fornecedorViewModel);
        }

        private async Task<FornecedorViewModel> FornecedorProdutosEndereco(Guid id)
        {
            return _mapper.Map<FornecedorViewModel>(await _fornecedorRespository.ObterFornecedorProdutosEndereco(id));
        }

        private async Task<FornecedorViewModel> FornecedorEndereco(Guid id)
        {
            return _mapper.Map<FornecedorViewModel>(await _fornecedorRespository.ObterFornecedorEndereco(id));
        }
    }
}