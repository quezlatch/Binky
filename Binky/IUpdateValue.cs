using System;
using System.Threading.Tasks;

namespace Binky
{

	public interface IUpdateValue<TKey, TValue>
	{
		Task<TValue> Get(TKey key);
	}
}
