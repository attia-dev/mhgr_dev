using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Topics;

namespace Nop.Web.Controllers;

/// <summary>
/// Reference / config data for the mobile app: countries + states (address forms), currencies, languages, CMS
/// topics (terms/privacy/about), and basic store info. Anonymous. Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
public partial class ReferenceApiController : Controller
{
    #region Fields

    protected readonly ICountryService _countryService;
    protected readonly ICurrencyService _currencyService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IStateProvinceService _stateProvinceService;
    protected readonly IStoreContext _storeContext;
    protected readonly ITopicService _topicService;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public ReferenceApiController(ICountryService countryService,
        ICurrencyService currencyService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IStateProvinceService stateProvinceService,
        IStoreContext storeContext,
        ITopicService topicService,
        IWorkContext workContext)
    {
        _countryService = countryService;
        _currencyService = currencyService;
        _languageService = languageService;
        _localizationService = localizationService;
        _stateProvinceService = stateProvinceService;
        _storeContext = storeContext;
        _topicService = topicService;
        _workContext = workContext;
    }

    #endregion

    #region Methods

    // GET /api/countries
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<CountryDto>), 200)]
    public virtual async Task<IActionResult> Countries()
    {
        var langId = (await _workContext.GetWorkingLanguageAsync()).Id;
        var countries = await _countryService.GetAllCountriesAsync(langId);

        var items = new List<CountryDto>();
        foreach (var c in countries)
            items.Add(new CountryDto
            {
                Id = c.Id,
                Name = await _localizationService.GetLocalizedAsync(c, x => x.Name),
                TwoLetterIsoCode = c.TwoLetterIsoCode,
                NumericIsoCode = c.NumericIsoCode.ToString()
            });

        return Json(new ListResultDto<CountryDto> { Items = items });
    }

    // GET /api/countries/{id}/states
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<StateProvinceDto>), 200)]
    public virtual async Task<IActionResult> States(int id)
    {
        var langId = (await _workContext.GetWorkingLanguageAsync()).Id;
        var states = await _stateProvinceService.GetStateProvincesByCountryIdAsync(id, langId);

        var items = new List<StateProvinceDto>();
        foreach (var s in states)
            items.Add(new StateProvinceDto { Id = s.Id, Name = await _localizationService.GetLocalizedAsync(s, x => x.Name) });

        return Json(new ListResultDto<StateProvinceDto> { Items = items });
    }

    // GET /api/currencies
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<CurrencyDto>), 200)]
    public virtual async Task<IActionResult> Currencies()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var currencies = await _currencyService.GetAllCurrenciesAsync(storeId: store.Id);
        return Json(new ListResultDto<CurrencyDto>
        {
            Items = currencies.Select(c => new CurrencyDto { Id = c.Id, Name = c.Name, Code = c.CurrencyCode }).ToList()
        });
    }

    // GET /api/languages
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<LanguageDto>), 200)]
    public virtual async Task<IActionResult> Languages()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var languages = await _languageService.GetAllLanguagesAsync(storeId: store.Id);
        return Json(new ListResultDto<LanguageDto>
        {
            Items = languages.Select(l => new LanguageDto { Id = l.Id, Name = l.Name, Code = l.UniqueSeoCode, Rtl = l.Rtl }).ToList()
        });
    }

    // GET /api/topics/{systemName}
    [HttpGet]
    [ProducesResponseType(typeof(TopicDto), 200)]
    public virtual async Task<IActionResult> Topic(string systemName)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var topic = await _topicService.GetTopicBySystemNameAsync(systemName, store.Id);
        if (topic == null || !topic.Published || topic.IsPasswordProtected)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.reference.topicnotfound") });

        return Json(new TopicDto
        {
            SystemName = topic.SystemName,
            Title = await _localizationService.GetLocalizedAsync(topic, x => x.Title),
            Body = await _localizationService.GetLocalizedAsync(topic, x => x.Body)
        });
    }

    // GET /api/store/info
    [HttpGet]
    [ProducesResponseType(typeof(StoreInfoDto), 200)]
    public virtual async Task<IActionResult> StoreInfo()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        return Json(new StoreInfoDto
        {
            Id = store.Id,
            Name = store.Name,
            Url = store.Url,
            Currency = (await _workContext.GetWorkingCurrencyAsync()).CurrencyCode
        });
    }

    #endregion
}
