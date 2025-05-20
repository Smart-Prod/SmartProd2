using MongoDB.Driver;

namespace SmartProd.Models
{
    public class ContextMongodb
    {
        public static string? ConnectionString { get; set; }
        public static string? Database { get; set; }
        public static bool IsSSL { get; set; }
        private IMongoDatabase _database { get; }


        public ContextMongodb()
        {
            try
            {
                MongoClientSettings settings = MongoClientSettings.
                    FromUrl(new MongoUrl(ConnectionString));

                if (IsSSL) 
                {
                    settings.SslSettings = new SslSettings 
                    {
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                    };
                }

                var mongoCliente = new MongoClient(settings);
                _database = mongoCliente.GetDatabase(Database);
            }
            catch(Exception)
            {
                throw new Exception("Não foi possivel conectar");
            }
            
        }

        public IMongoCollection<Empresa> Emrpesa
        {
            get 
            {
                return _database.GetCollection<Empresa>("Empresa");
            }
        }
        public IMongoCollection<Produto> Produto
        {
            get
            {
                return _database.GetCollection<Produto>("Produto");
            }
        }

        public IMongoCollection<Estoque> Estoque
        {
            get
            {
                return _database.GetCollection<Estoque>("Estoque");
            }
        }

        public IMongoCollection<NotaEntrega> NotaEntrega
        {
            get
            {
                return _database.GetCollection<NotaEntrega>("NotaEntrega");
            }
        }

        public IMongoCollection<NotaSaida> NotaSaida
        {
            get
            {
                return _database.GetCollection<NotaSaida>("NotaSaida");
            }
        }


    }
}
