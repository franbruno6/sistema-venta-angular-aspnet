using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using SistemaVenta.BLL.Servicios.Contrato;
using SistemaVenta.DAL.Repositorios.Contrato;
using SistemaVenta.DTO;
using SistemaVenta.Model;

namespace SistemaVenta.BLL.Servicios
{
    public class DashboardService : IDashboardService
    {
        private readonly IVentaRepository _ventaRepositorio;
        private readonly IGenericRepository<Producto> _productoRepositorio;
        private readonly IMapper _mapper;

        public DashboardService(IVentaRepository ventaRepositorio, IGenericRepository<Producto> productoRepositorio, IMapper mapper)
        {
            _ventaRepositorio = ventaRepositorio;
            _productoRepositorio = productoRepositorio;
            _mapper = mapper;
        }

        private IQueryable<Venta> RetornarVentas(IQueryable<Venta> tablaVenta, int restarCantidadDias)
        {
            DateTime? ultimaFecha = tablaVenta
                .OrderByDescending(v=> v.FechaRegistro)
                .Select(v=> v.FechaRegistro)
                .First();
            
            ultimaFecha = ultimaFecha.Value.AddDays(restarCantidadDias);

            return tablaVenta.Where(v=> v.FechaRegistro >= ultimaFecha);
        }
        private async Task<int> TotalVentasUltimaSemana()
        {
            int total = 0;
            IQueryable<Venta> ventaQuery = await _ventaRepositorio.Consultar();

            if(ventaQuery.Count() > 0)
            {
                var tablaVenta = RetornarVentas(ventaQuery, -7);
                total = tablaVenta.Count();
            }
            return total;
        }
        private async Task<string> TotalIngresosUltimaSemana()
        {
            decimal resultado = 0;
            IQueryable<Venta> ventaQuery = await _ventaRepositorio.Consultar();

            if (ventaQuery.Count() > 0)
            {
                var tablaVenta = RetornarVentas(ventaQuery, -7);
                resultado = tablaVenta.Select(v => v.Total).Sum(v=> v.Value);
            }
            return Convert.ToString(resultado, new CultureInfo("es-AR"));
        }

        private async Task<int> TotalProductos()
        {
            IQueryable<Producto> productoQuery = await _productoRepositorio.Consultar();

            int total = productoQuery.Count();
            return total;
        }
        private async Task<Dictionary<string,int>> VentasUltimaSemana()
        {
            Dictionary<string, int> resultado = new Dictionary<string, int>();

            IQueryable<Venta> ventaQuery = await _ventaRepositorio.Consultar();

            if(ventaQuery.Count() > 0)
            {
                var tablaVenta = RetornarVentas(ventaQuery, -7);

                resultado = tablaVenta
                    .GroupBy(v => v.FechaRegistro.Value.Date)
                    .OrderBy(v => v.Key)
                    .Select(dv => new { Fecha = dv.Key.ToString("dd/MM/yyyy"), Total = dv.Count() })
                    .ToDictionary(keySelector: r => r.Fecha, elementSelector: r => r.Total);
            }

            return resultado;
        }

        public async Task<DashboardDTO> Resumen()
        {
            DashboardDTO vmDashboard = new DashboardDTO();
            
            try
            {
                vmDashboard.TotalVentas = await TotalVentasUltimaSemana();
                vmDashboard.TotalIngresos = await TotalIngresosUltimaSemana();
                vmDashboard.TotalProductos = await TotalProductos();

                List<VentaSemanaDTO> listaVentaSemana = new List<VentaSemanaDTO>();

                foreach (KeyValuePair<string, int> item in await VentasUltimaSemana())
                {
                    listaVentaSemana.Add(new VentaSemanaDTO()
                    {
                        Fecha = item.Key,
                        Total = item.Value
                    });
                }

                vmDashboard.VentasUltimaSemana = listaVentaSemana;
                return vmDashboard;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
