package flabbergast;

public class CheckResult extends Computation {
	private boolean success;

	private Class<? extends Computation> test_target;

	public CheckResult(TaskMaster task_master,
			Class<? extends Computation> test_target) {
		super(task_master);
		this.test_target = test_target;
	}

	public boolean getSuccess() {
		return success;
	}

	@Override
	protected void run() {
		try {
			Computation computation = test_target.getConstructor(
					TaskMaster.class).newInstance(task_master);
			computation.listen(new ConsumeResult() {

				@Override
				public void consume(Object result) {
					if (result instanceof Frame) {
						Lookup lookup = new Lookup(task_master, null,
								new String[]{"value"}, ((Frame) result)
										.getContext());
						lookup.listen(new ConsumeResult() {

							@Override
							public void consume(Object result) {
								if (result == null) {
									return;
								}
								if (result instanceof Boolean) {
									success = (Boolean) result;
								}

							}
						});
					}
				}
			});
		} catch (Exception e) {
		}

	}
}
