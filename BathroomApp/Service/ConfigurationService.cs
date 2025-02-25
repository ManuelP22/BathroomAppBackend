namespace BathroomApp.Service
{

    public interface IConfigurationService
    {
        int GetFinalAlert();
        int GetFirstAlert();
        int GetSecondAlert();
        int GetThirdAlert();
    }

    public class ConfigurationService(IConfiguration configuration) : IConfigurationService
    {
        private readonly IConfiguration _configuration = configuration;

        public int GetFirstAlert()
        {
            return _configuration.GetValue<int>("FirstOccupiedAlert");
        }

        public int GetSecondAlert()
        {
            return _configuration.GetValue<int>("SecondOccupiedAlert");
        }

        public int GetThirdAlert()
        {
            return _configuration.GetValue<int>("ThirdOccupiedAlert");
        }

        public int GetFinalAlert()
        {
            return _configuration.GetValue<int>("FreeOccupiedAlert");
        }
    }
}
