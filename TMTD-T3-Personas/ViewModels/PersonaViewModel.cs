using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TMTD_T3_Personas.Data;
using TMTD_T3_Personas.Models;
using System.IO;

namespace TMTD_T3_Personas.ViewModels
{
    public class PersonaViewModel : INotifyPropertyChanged
    {
        private PersonaRepositoryActions _remoteRepo;
        private PersonaLocalRepository _localRepo;
        private static int _nextNegativeId = -1;  // Contador para IDs negativos temporales

        // ---------------------------------------------------------------
        // LISTA
        // ---------------------------------------------------------------
        public ObservableCollection<Persona> Personas { get; set; } = new();

        // ---------------------------------------------------------------
        // FORMULARIO
        // ---------------------------------------------------------------
        private Persona _personaActual = new();
        public Persona PersonaActual
        {
            get => _personaActual;
            set { _personaActual = value; OnPropertyChanged(); }
        }

        // ---------------------------------------------------------------
        // ESTADO
        // ---------------------------------------------------------------
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private string _mensaje = "";
        public string Mensaje
        {
            get => _mensaje;
            set { _mensaje = value; OnPropertyChanged(); }
        }

        private string _rutaBD = "";
        public string RutaBD
        {
            get => _rutaBD;
            set { _rutaBD = value; OnPropertyChanged(); }
        }

        // ---------------------------------------------------------------
        // COMANDOS
        // ---------------------------------------------------------------
        public ICommand SincronizarCommand { get; private set; }
        public ICommand GuardarCommand { get; private set; }
        public ICommand EliminarCommand { get; private set; }
        public ICommand LimpiarCommand { get; private set; }
        public ICommand CopiarRutaCommand { get; private set; }

        // ---------------------------------------------------------------
        // CONSTRUCTOR
        // ---------------------------------------------------------------
        public PersonaViewModel()
        {
            // Obtener ruta de la BD
            RutaBD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "personas.db3");

            // Si la ruta no existe, buscar en la ubicación de WinUI
            if (!File.Exists(RutaBD))
            {
                // Para aplicaciones MAUI en Windows, a veces está en Packages
                string packagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
                if (Directory.Exists(packagesPath))
                {
                    var directories = Directory.GetDirectories(packagesPath, "*TMTD_T3_Personas*");
                    if (directories.Length > 0)
                    {
                        RutaBD = Path.Combine(directories[0], "LocalState", "personas.db3");
                    }
                }
            }

            SincronizarCommand = new Command(async () => await SincronizarAsync(), () => !IsBusy && _localRepo != null && _remoteRepo != null);
            GuardarCommand = new Command(async () => await GuardarAsync(), () => !IsBusy && _localRepo != null);
            EliminarCommand = new Command(async () => await EliminarAsync(), () => !IsBusy && _localRepo != null);
            LimpiarCommand = new Command(() =>
            {
                PersonaActual = new Persona();
                Mensaje = "";
            });
            CopiarRutaCommand = new Command(() =>
            {
                Clipboard.SetTextAsync(RutaBD);
                Mensaje = $"Ruta copiada al portapapeles: {RutaBD}";
            });

            // Iniciar la base de datos en segundo plano
            Task.Run(async () => await InicializarBaseDatosAsync());
        }

        // ---------------------------------------------------------------
        // INICIALIZACIÓN ASÍNCRONA DE LA BASE DE DATOS LOCAL
        // ---------------------------------------------------------------
        private async Task InicializarBaseDatosAsync()
        {
            try
            {
                var dbService = new DatabaseService();
                await dbService.InitAsync();   // No bloquea el UI
                _localRepo = new PersonaLocalRepository(dbService);
                _remoteRepo = new PersonaRepositoryActions();

                // Cargar datos locales en el hilo UI
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await CargarLocalesAsync();
                    RefrescarComandos(); // Activa los botones
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Mensaje = $"Error inicializando BD: {ex.Message}";
                });
            }
        }

        // ---------------------------------------------------------------
        // CARGAR DATOS LOCALES (solo no eliminados)
        // ---------------------------------------------------------------
        private async Task CargarLocalesAsync()
        {
            IsBusy = true;
            try
            {
                var locales = await _localRepo.GetPersonasAsync();
                Personas.Clear();
                foreach (var p in locales.Where(p => !p.Eliminado))
                    Personas.Add(p);
                Mensaje = $"{Personas.Count} personas cargadas (locales).";
            }
            catch (Exception ex)
            {
                Mensaje = $"Error cargando locales: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        // ---------------------------------------------------------------
        // GUARDAR (local siempre)
        // ---------------------------------------------------------------
        private async Task GuardarAsync()
        {
            if (string.IsNullOrWhiteSpace(PersonaActual.Nombre))
            {
                Mensaje = "El nombre es obligatorio.";
                return;
            }

            IsBusy = true;
            RefrescarComandos();

            try
            {
                PersonaActual.FechaModificacion = DateTime.Now;

                if (PersonaActual.IdPersona == 0)
                {
                    // Nuevo registro: asignar ID negativo temporal único
                    PersonaActual.IdPersona = _nextNegativeId--;
                    PersonaActual.EsLocal = true;
                    PersonaActual.PendienteSync = true;
                    PersonaActual.Eliminado = false;

                    await _localRepo.InsertarPersonaAsync(PersonaActual);
                    Mensaje = "Persona agregada localmente (pendiente de sincronización).";
                }
                else
                {
                    // Actualización de registro existente
                    PersonaActual.PendienteSync = true;
                    await _localRepo.ActualizarPersonaAsync(PersonaActual);
                    Mensaje = "Persona actualizada localmente (pendiente de sincronización).";
                }

                PersonaActual = new Persona();
                await CargarLocalesAsync(); // Refresca la lista
            }
            catch (Exception ex)
            {
                Mensaje = $"Error al guardar: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        // ---------------------------------------------------------------
        // ELIMINAR (marca como eliminado y sincroniza luego)
        // ---------------------------------------------------------------
        private async Task EliminarAsync()
        {
            if (PersonaActual.IdPersona == 0)
            {
                Mensaje = "Selecciona una persona de la lista para eliminar.";
                return;
            }

            IsBusy = true;
            RefrescarComandos();

            try
            {
                if (PersonaActual.IdPersona > 0)
                {
                    // Si ya tiene ID positivo (sincronizado o de nube), marcamos como eliminado pendiente
                    PersonaActual.Eliminado = true;
                    PersonaActual.PendienteSync = true;
                    await _localRepo.ActualizarPersonaAsync(PersonaActual);
                    Mensaje = "Persona marcada para eliminar (se borrará al sincronizar).";
                }
                else
                {
                    // Si es un registro local negativo (no sincronizado), lo eliminamos directamente
                    await _localRepo.EliminarPersonaAsync(PersonaActual.IdPersona);
                    Mensaje = "Persona eliminada localmente (nunca se sincronizará).";
                }

                PersonaActual = new Persona();
                await CargarLocalesAsync(); // Refresca la lista
            }
            catch (Exception ex)
            {
                Mensaje = $"Error al eliminar: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        // ---------------------------------------------------------------
        // SINCRONIZACIÓN BIDIRECCIONAL
        // ---------------------------------------------------------------
        private async Task SincronizarAsync()
        {
            IsBusy = true;
            RefrescarComandos();
            Mensaje = "Iniciando sincronización...";

            try
            {
                // Mostrar ruta de la BD
                Mensaje = $"BD Local: {RutaBD}";

                // 1. PRIMERO: Obtener todos los registros locales actuales
                var localesAntes = await _localRepo.GetPersonasAsync();

                // 2. SUBIR cambios locales pendientes a SQL Server
                var pendientes = await _localRepo.ObtenerPendientesSyncAsync();

                foreach (var local in pendientes)
                {
                    if (local.Eliminado)
                    {
                        // Eliminar de la nube
                        await _remoteRepo.EliminarPersonaAsync(local.IdPersona);
                        // Eliminar localmente
                        await _localRepo.EliminarPersonaAsync(local.IdPersona);
                    }
                    else if (local.IdPersona < 0)
                    {
                        // Nuevo registro local: insertar en nube
                        int nuevoId = await _remoteRepo.InsertarPersonaYDevolverIdAsync(local);

                        // Eliminar el registro local con ID negativo
                        await _localRepo.EliminarPersonaAsync(local.IdPersona);

                        // Crear nuevo registro con el ID de la nube
                        var nuevoRegistro = new Persona
                        {
                            IdPersona = nuevoId,
                            Nombre = local.Nombre,
                            Apellido = local.Apellido,
                            Email = local.Email,
                            Telefono = local.Telefono,
                            FechaNacimiento = local.FechaNacimiento,
                            Activo = local.Activo,
                            EsLocal = false,
                            PendienteSync = false,
                            Eliminado = false,
                            FechaModificacion = DateTime.Now
                        };

                        await _localRepo.InsertarPersonaAsync(nuevoRegistro);
                    }
                    else
                    {
                        // Actualización de registro existente
                        await _remoteRepo.ActualizarPersonaAsync(local);
                        local.PendienteSync = false;
                        await _localRepo.ActualizarPersonaAsync(local);
                    }
                }

                // 3. BAJAR datos de SQL Server
                var remotas = await _remoteRepo.GetPersonasAsync();

                // 4. Obtener locales actualizados (después de las operaciones anteriores)
                var localesActuales = await _localRepo.GetPersonasAsync();
                var localesDict = localesActuales.ToDictionary(p => p.IdPersona);

                // 5. Sincronizar: actualizar locales con datos de nube
                foreach (var remota in remotas)
                {
                    if (localesDict.TryGetValue(remota.IdPersona, out var local))
                    {
                        // Existe en local - actualizar solo si local no tiene cambios pendientes
                        if (!local.PendienteSync && local.FechaModificacion < remota.FechaModificacion)
                        {
                            local.Nombre = remota.Nombre;
                            local.Apellido = remota.Apellido;
                            local.Email = remota.Email;
                            local.Telefono = remota.Telefono;
                            local.FechaNacimiento = remota.FechaNacimiento;
                            local.Activo = remota.Activo;
                            local.FechaModificacion = remota.FechaModificacion;
                            local.EsLocal = false;
                            local.PendienteSync = false;
                            local.Eliminado = false;
                            await _localRepo.ActualizarPersonaAsync(local);
                        }
                    }
                    else
                    {
                        // No existe en local - insertar
                        remota.EsLocal = false;
                        remota.PendienteSync = false;
                        remota.Eliminado = false;
                        if (remota.FechaModificacion == default)
                            remota.FechaModificacion = DateTime.Now;

                        // Verificar que no exista ya (evitar duplicados)
                        var existe = await _localRepo.ObtenerPersonaPorIdAsync(remota.IdPersona);
                        if (existe == null)
                        {
                            await _localRepo.InsertarPersonaAsync(remota);
                        }
                    }
                }

                Mensaje = $"Sincronización completada. Remotos: {remotas.Count}, Locales: {localesActuales.Count}";
                await CargarLocalesAsync();
            }
            catch (Exception ex)
            {
                Mensaje = $"Error en sincronización: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }
        // ---------------------------------------------------------------
        // AUXILIAR: reactiva los botones después de cada operación
        // ---------------------------------------------------------------
        private void RefrescarComandos()
        {
            (SincronizarCommand as Command)?.ChangeCanExecute();
            (GuardarCommand as Command)?.ChangeCanExecute();
            (EliminarCommand as Command)?.ChangeCanExecute();
        }

        // ---------------------------------------------------------------
        // INotifyPropertyChanged
        // ---------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}