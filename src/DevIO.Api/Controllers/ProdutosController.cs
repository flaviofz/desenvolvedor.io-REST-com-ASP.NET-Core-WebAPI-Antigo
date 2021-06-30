using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using DevIO.Api.ViewModels;
using DevIO.Business.Intefaces;
using DevIO.Business.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevIO.Api.Controllers
{
    [Route("api/[controller]")]
    public class ProdutosController : MainController
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IProdutoService _produtoService;
        private readonly IMapper _mapper;
        public ProdutosController(
            IMapper mapper,
            INotificador notificador, 
            IProdutoRepository produtoRepository, 
            IProdutoService produtoService) : base(notificador)
        {
            _mapper = mapper;
            _produtoRepository = produtoRepository;
            _produtoService = produtoService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProdutoViewModel>>> ObterTodos()
        {
            return CustomResponse(_mapper.Map<IEnumerable<ProdutoViewModel>>(await _produtoRepository.ObterProdutosFornecedores()));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProdutoViewModel>> ObterPorId(Guid id)
        {
            var produtoViewModel = await ObterProduro(id);

            if (produtoViewModel == null) return NotFound();

            return CustomResponse(produtoViewModel);
        }

        [HttpPost]
        public async Task<ActionResult<ProdutoViewModel>> Adicionar(ProdutoViewModel produtoViewModel)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var imagemNome = Guid.NewGuid() + "_" + produtoViewModel.Imagem;
            produtoViewModel.Imagem = imagemNome;

            if (!UploadArquivo(produtoViewModel.ImagemUpload, imagemNome))
                return CustomResponse(produtoViewModel);

            await _produtoService.Adicionar(_mapper.Map<Produto>(produtoViewModel));

            return CustomResponse(produtoViewModel);
        }

        // [DisableRequestSizeLimit] // Desabilita o limite de tamanho do request (consequentemente da imagem também)
        // //[DisableRequestSizeLimit(40000000)] // 40mb de limite
        // [HttpPost("Adicionar")]
        // public async Task<ActionResult<ProdutoViewModel>> AdicionarAlternativo(
        //     ProdutoImagemViewModel produtoImagemViewModel
        // )
        // {
        //     if (!ModelState.IsValid) return CustomResponse(ModelState);

        //     var imagemPrefixo = Guid.NewGuid() + "_";
        //     produtoImagemViewModel.Imagem = imagemPrefixo + produtoImagemViewModel.ImagemUpload.FileName;

        //     if (!await UploadArquivoAlternativo(produtoImagemViewModel.ImagemUpload, imagemPrefixo))
        //         return CustomResponse(produtoImagemViewModel);

        //     await _produtoService.Adicionar(_mapper.Map<Produto>(produtoImagemViewModel));

        //     return CustomResponse(produtoImagemViewModel);
        // }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(
            Guid id, 
            ProdutoViewModel produtoViewModel
        )
        {
            if (id != produtoViewModel.Id)
            {
                NotificarErro("Os ids informados não são iguais!");
                return CustomResponse();
            }

            var produtoAtualizacao = await ObterProduto(id);

            if (string.IsNullOrEmpty(produtoViewModel.Imagem))
                produtoViewModel.Imagem = produtoAtualizacao.Imagem;

            if (!ModelState.IsValid) return CustomResponse(ModelState);

            if (produtoViewModel.ImagemUpload != null)
            {
                var imagemNome = Guid.NewGuid() + "_" + produtoViewModel.Imagem;
                if (!UploadArquivo(produtoViewModel.ImagemUpload, imagemNome))
                {
                    return CustomResponse(ModelState);
                }

                produtoAtualizacao.Imagem = imagemNome;
            }

            produtoAtualizacao.FornecedorId = produtoViewModel.FornecedorId;
            produtoAtualizacao.Nome = produtoViewModel.Nome;
            produtoAtualizacao.Descricao = produtoViewModel.Descricao;
            produtoAtualizacao.Valor = produtoViewModel.Valor;
            produtoAtualizacao.Ativo = produtoViewModel.Ativo;

            await _produtoService.Atualizar(_mapper.Map<Produto>(produtoAtualizacao));

            return CustomResponse(produtoViewModel);
        }

        private async Task<ProdutoViewModel> ObterProduto(Guid id)
        {
            return _mapper.Map<ProdutoViewModel>(await _produtoRepository.ObterProdutoFornecedor(id));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ProdutoViewModel>> Excluir(Guid id)
        {
            var produtoViewModel = await ObterProduro(id);

            if (produtoViewModel == null) return NotFound();

            await _produtoService.Remover(id);

            return CustomResponse(produtoViewModel);
        }

        private bool UploadArquivo(
            string arquivo, 
            string imgNome
        )
        {
            var imageDataByteArray = Convert.FromBase64String(arquivo);

            if (string.IsNullOrEmpty(arquivo))
            {
                NotificarErro("Forneça uma imagem para este produto!");
                return false;
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens", imgNome);

            if (System.IO.File.Exists(filePath))
            {
                NotificarErro("Já existe um arquivo com este nome!");
                return false;
            }

            System.IO.File.WriteAllBytes(filePath, imageDataByteArray);

            return true;
        }

        // private async Task<bool> UploadArquivoAlternativo(
        //     IFormFile arquivo,
        //     string imgPrefixo
        // )
        // {
        //     if (arquivo == null || arquivo.Length == 0)
        //     {
        //         NotificarErro("Forneça uma imagem para este produto!");
        //         return false;
        //     }

        //     var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens", imgPrefixo + arquivo.FileName);

        //     if (System.IO.File.Exists(path))
        //     {
        //         NotificarErro("Já existe um arquivo com este nome!");
        //         return false;
        //     }

        //     using (var stream = new FileStream(path, FileMode.Create))
        //         await arquivo.CopyToAsync(stream);

        //     return true;
        // }

        private async Task<ProdutoViewModel> ObterProduro(Guid id)
        {
            return _mapper.Map<ProdutoViewModel>(await _produtoRepository.ObterProdutoFornecedor(id));      
        }

        #region UploadAlternativo

        [RequestSizeLimit(40000000)]
        [HttpPost("Adicionar")]
        public async Task<ActionResult<ProdutoViewModel>> AdicionarAlternativo(
            ProdutoImagemViewModel produtoViewModel
        )
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var imgPrefixo = Guid.NewGuid() + "_";
            if (!await UploadArquivoAlternativo(produtoViewModel.ImagemUpload, imgPrefixo))
            {
                return CustomResponse(ModelState);
            }

            produtoViewModel.Imagem = imgPrefixo + produtoViewModel.ImagemUpload.FileName;
            await _produtoService.Adicionar(_mapper.Map<Produto>(produtoViewModel));

            return CustomResponse(produtoViewModel);
        }
        
        [RequestSizeLimit(40000000)]
        //[DisableRequestSizeLimit]
        [HttpPost("imagem")]
        public ActionResult AdicionarImagem(IFormFile file)
        {
            return Ok(file);
        }

        private async Task<bool> UploadArquivoAlternativo(IFormFile arquivo, string imgPrefixo)
        {
            if (arquivo == null || arquivo.Length == 0)
            {
                NotificarErro("Forneça uma imagem para este produto!");
                return false;
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imgPrefixo + arquivo.FileName);

            if (System.IO.File.Exists(path))
            {
                NotificarErro("Já existe um arquivo com este nome!");
                return false;
            }

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return true;
        }

        #endregion
    }
}