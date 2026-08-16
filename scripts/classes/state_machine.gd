class_name StateMachine
## A state machine needless of an enum and operates on function pointers directly.
## Constructor takes a function pointer to the default state callable,
## next state can be called using `call_next_state`...

var _default_callable: Callable
var _next_callable: Callable

func _init(default_state_callable: Callable) -> void:
	_default_callable = default_state_callable
	_next_callable = _default_callable


## Sets the callable to be called by `call_next_state`.
func set_state(next_state_callable: Callable) -> void:
	_next_callable = next_state_callable


## Same as `set_state(something);` `call_next_state()`
func set_state_and_call(next_state_callable: Callable) -> void:
	set_state(next_state_callable)
	call_next_state()


## Returns the callable to be called by `call_next_state`.
func get_state() -> Callable:
	return _next_callable


## Sets the state to the one passed to the constructor, or set by `set_default_state`.
func reset_state() -> void:
	_next_callable = _default_callable


func set_default_state(next_state_callable: Callable) -> void:
	_default_callable = next_state_callable


func call_next_state():
	return _next_callable.call()


func is_in_default_state() -> bool:
	return _next_callable == _default_callable
